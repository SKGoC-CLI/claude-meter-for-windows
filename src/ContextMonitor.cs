using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeMeter;

sealed record SessionContext(
    string Project,
    string Model,
    long Tokens,
    long WindowSize,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActive)
{
    public double Percent => Math.Clamp(Tokens * 100.0 / WindowSize, 0, 100);
}

/// <summary>
/// Reads the context-window fill of the most recently active Claude Code
/// session from its local transcript (~/.claude/projects/**/*.jsonl).
/// Read-only, no network; safe to call every poll.
/// </summary>
static class ContextMonitor
{
    static readonly string ProjectsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

    /// <summary>
    /// Active sessions (transcripts written within <paramref name="maxIdle"/>), sorted and
    /// capped to <paramref name="max"/>. sort: "active" (most-recent first) | "name" | "context".
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

        return new SessionContext(project, FriendlyModel(modelId), tokens, window, startedAt, new DateTimeOffset(file.LastWriteTime));
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
