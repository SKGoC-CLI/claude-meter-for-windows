using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeMeter;

sealed record SessionContext(
    string Project,
    string Model,
    long Tokens,
    long WindowSize,
    DateTimeOffset StartedAt)
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

    public static SessionContext? GetActive(TimeSpan maxIdle)
    {
        try
        {
            if (!Directory.Exists(ProjectsDir)) return null;

            var file = Directory.EnumerateFiles(ProjectsDir, "*.jsonl", SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .Where(f => DateTime.Now - f.LastWriteTime < maxIdle && f.Length > 0)
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            if (file is null) return null;

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
            var startedAt = file.CreationTime;
            var m = Regex.Match(head, "\"timestamp\":\\s*\"([^\"]+)\"");
            if (m.Success && DateTimeOffset.TryParse(m.Groups[1].Value, out var ts))
                return new SessionContext(project, FriendlyModel(modelId), tokens, window, ts.ToLocalTime());

            return new SessionContext(project, FriendlyModel(modelId), tokens, window, startedAt);
        }
        catch
        {
            return null; // best-effort; never disturb the app
        }
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
