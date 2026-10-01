using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClaudeMeter;

sealed record SessionContext(
    string Project,
    string Model,
    long Tokens,
    long WindowSize,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActive,
    SessionState State = SessionState.Working)
{
    public double Percent => Math.Clamp(Tokens * 100.0 / WindowSize, 0, 100);
}

enum SessionState { Working, Waiting, Idle }

/// <summary>
/// Reads the context-window fill of the most recently active Claude Code
/// session from its local transcript (~/.claude/projects/**/*.jsonl).
/// Read-only, no network; safe to call every poll.
/// </summary>
static class ContextMonitor
{
    static readonly string ProjectsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

    /// <summary>Turn ended and the user was last seen within this → Waiting, else Idle.</summary>
    public static readonly TimeSpan WaitingFor = TimeSpan.FromMinutes(10);

    /// <summary>Sessions with no conversation activity for this long are dropped (even Working: a crashed one must go).</summary>
    public static readonly TimeSpan DropAfter = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Active sessions (transcripts written within <paramref name="maxIdle"/> — a cheap
    /// LastWriteTime pre-filter; the real cut is DropAfter on the last transcript timestamp),
    /// sorted and capped to <paramref name="max"/>. sort: "active" (most-recent first) | "name" | "context".
    /// </summary>
    public static IReadOnlyList<SessionContext> GetActive(TimeSpan maxIdle, int max, string sort)
    {
        try
        {
            if (!Directory.Exists(ProjectsDir)) return Array.Empty<SessionContext>();

            var sessions = Directory.EnumerateFiles(ProjectsDir, "*.jsonl", SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .Where(f => DateTime.Now - f.LastWriteTime < maxIdle && f.Length > 0)
                .OrderByDescending(f => f.LastWriteTime)
                .Take(12) // cap parse cost; >12 sessions active within maxIdle is unrealistic
                .Select(Parse)
                .Where(c => c is not null)
                .Select(c => c!);

            IEnumerable<SessionContext> sorted = sort switch
            {
                "name" => sessions.OrderBy(c => c.Project, StringComparer.OrdinalIgnoreCase),
                "context" => sessions.OrderByDescending(c => c.Percent),
                _ => sessions.OrderByDescending(c => c.LastActive), // "active"
            };
            return sorted.Take(Math.Max(1, max)).ToList();
        }
        catch
        {
            return Array.Empty<SessionContext>(); // best-effort; never disturb the app
        }
    }

    static SessionContext? Parse(FileInfo file)
    {
        try
        {
            string tail = ReadChunk(file, fromEnd: true, 128 * 1024);

            long input = LastLong(tail, "\"input_tokens\":\\s*(\\d+)");
            long cacheCreate = LastLong(tail, "\"cache_creation_input_tokens\":\\s*(\\d+)");
            long cacheRead = LastLong(tail, "\"cache_read_input_tokens\":\\s*(\\d+)");
            long tokens = input + cacheCreate + cacheRead;
            if (tokens <= 0) return null;

            string modelId = LastString(tail, "\"model\":\\s*\"([^\"]+)\"") ?? "";
            // ponytail: transcript ไม่ระบุ window; Opus/Sonnet ปัจจุบัน = 1M, Haiku = 200k.
            // เดาจาก token ไม่ได้ (จะผิดทุกครั้งที่ยังไม่ถึง 200k บน account ที่ได้ 1M)
            long window = modelId.Contains("haiku", StringComparison.OrdinalIgnoreCase) ? 200_000 : 1_000_000;
            if (tokens > window) window = 1_000_000; // safety: ห้าม % เกิน 100 เพราะเดา window ต่ำไป

            string cwd = LastString(tail, "\"cwd\":\\s*\"((?:[^\"\\\\]|\\\\.)+)\"") ?? "";
            string project = cwd.Length > 0
                ? Path.GetFileName(cwd.Replace("\\\\", "\\").TrimEnd('\\', '/'))
                : file.Directory?.Name ?? "?";

            string head = ReadChunk(file, fromEnd: false, 8 * 1024);
            var startedAt = new DateTimeOffset(file.CreationTime);
            var m = Regex.Match(head, "\"timestamp\":\\s*\"([^\"]+)\"");
            if (m.Success && DateTimeOffset.TryParse(m.Groups[1].Value, out var ts))
                startedAt = ts.ToLocalTime();

            // file LastWriteTime is unreliable: metadata lines (no timestamp) are appended
            // long after the conversation goes quiet, so use the last line's own timestamp
            var (ended, lastTs) = Classify(tail);
            var active = lastTs ?? new DateTimeOffset(file.LastWriteTime);
            var age = DateTimeOffset.Now - active;
            if (age > DropAfter) return null;
            var state = !ended ? SessionState.Working : age <= WaitingFor ? SessionState.Waiting : SessionState.Idle;

            return new SessionContext(project, FriendlyModel(modelId), tokens, window, startedAt, active, state);
        }
        catch
        {
            // per-file best-effort: one unreadable transcript (rotated/locked mid-read)
            // must not blank the sessions that parsed fine
            return null;
        }
    }

    /// <summary>
    /// Walks the tail from the end: ended = the first conversational line closes a turn (or
    /// waits on the user); ts = the last parseable timestamp. Defaults to (false, null).
    /// </summary>
    static (bool ended, DateTimeOffset? ts) Classify(string tail)
    {
        bool? ended = null;
        DateTimeOffset? ts = null;
        try
        {
            var lines = tail.Split('\n');
            for (int i = lines.Length - 1; i >= 1 && (ended is null || ts is null); i--) // [0] may be a partial line
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                JsonDocument doc;
                try { doc = JsonDocument.Parse(line); } catch (JsonException) { continue; }
                using (doc)
                {
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object) continue;
                    if (ts is null && root.TryGetProperty("timestamp", out var t) && t.ValueKind == JsonValueKind.String
                        && DateTimeOffset.TryParse(t.GetString(), out var parsed))
                        ts = parsed.ToLocalTime();
                    if (ended is null)
                    {
                        string? type = root.TryGetProperty("type", out var ty) && ty.ValueKind == JsonValueKind.String ? ty.GetString() : null;
                        if (type is "assistant" or "user" or "system" or "attachment")
                            ended = IsTurnEnd(type, root);
                    }
                }
            }
        }
        catch
        {
            return (false, null); // default to Working
        }
        return (ended ?? false, ts);
    }

    static bool IsTurnEnd(string type, JsonElement root)
    {
        if (type == "system")
            return root.TryGetProperty("subtype", out var st) && st.ValueKind == JsonValueKind.String
                && st.GetString() is "stop_hook_summary" or "turn_duration";
        if (type is not ("assistant" or "user")
            || !root.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object)
            return false;

        if (type == "assistant")
        {
            if (msg.TryGetProperty("stop_reason", out var sr) && sr.ValueKind == JsonValueKind.String
                && sr.GetString() is "end_turn" or "stop_sequence")
                return true;
            // waiting on the user's answer
            if (msg.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() > 0)
            {
                var last = c[c.GetArrayLength() - 1];
                return last.TryGetProperty("type", out var bt) && bt.GetString() == "tool_use"
                    && last.TryGetProperty("name", out var nm) && nm.GetString() is "AskUserQuestion" or "ExitPlanMode";
            }
            return false;
        }

        // user: only an interrupt marker ends the turn
        if (!msg.TryGetProperty("content", out var uc)) return false;
        string? text = uc.ValueKind == JsonValueKind.String ? uc.GetString()
            : uc.ValueKind == JsonValueKind.Array && uc.GetArrayLength() > 0
              && uc[0].ValueKind == JsonValueKind.Object && uc[0].TryGetProperty("text", out var tx) ? tx.GetString()
            : null;
        return text?.StartsWith("[Request interrupted", StringComparison.Ordinal) == true;
    }

    static string ReadChunk(FileInfo file, bool fromEnd, int size)
    {
        using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        int len = (int)Math.Min(size, fs.Length);
        if (fromEnd) fs.Seek(-len, SeekOrigin.End);
        var buf = new byte[len];
        int read = fs.Read(buf, 0, len);
        return Encoding.UTF8.GetString(buf, 0, read);
    }

    static long LastLong(string text, string pattern)
    {
        var matches = Regex.Matches(text, pattern);
        return matches.Count > 0 ? long.Parse(matches[^1].Groups[1].Value) : 0;
    }

    static string? LastString(string text, string pattern)
    {
        var matches = Regex.Matches(text, pattern);
        return matches.Count > 0 ? matches[^1].Groups[1].Value : null;
    }

    static string FriendlyModel(string id) =>
        id.Contains("fable", StringComparison.OrdinalIgnoreCase) ? "Fable" :
        id.Contains("opus", StringComparison.OrdinalIgnoreCase) ? "Opus" :
        id.Contains("sonnet", StringComparison.OrdinalIgnoreCase) ? "Sonnet" :
        id.Contains("haiku", StringComparison.OrdinalIgnoreCase) ? "Haiku" :
        id.Length > 0 ? id : "?";
}
