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
    SessionState State = SessionState.Working,
    Goal? Goal = null)
{
    public double Percent => Math.Clamp(Tokens * 100.0 / WindowSize, 0, 100);
}

enum SessionState { Working, Asking, Waiting, Idle }

/// <summary>The session's step list: Done of Total finished; Current = the step being worked on (null when all done).</summary>
sealed record Goal(int Done, int Total, string? Current);

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
                // subagent transcripts end on a tool call by design (never "Waiting") and
                // aren't sessions the user answers — the parent session already shows the work
                .Where(f => f.Directory?.Name != "subagents")
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
            string tail = ReadChunk(file, fromEnd: true, TailBytes);

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
            var (ended, asking, lastTs) = Classify(tail, wholeFile: file.Length <= TailBytes);
            if (lastTs is null && file.Length > TailBytes) // last line(s) longer than the tail
                (ended, asking, lastTs) = Classify(ReadChunk(file, fromEnd: true, 2 * 1024 * 1024), file.Length <= 2 * 1024 * 1024);
            var active = lastTs ?? new DateTimeOffset(file.LastWriteTime);
            var age = DateTimeOffset.Now - active;
            if (age > DropAfter) return null;
            var state = !ended ? SessionState.Working : asking ? SessionState.Asking : age <= WaitingFor ? SessionState.Waiting : SessionState.Idle;

            // effort lives on every user line ("effort":"medium"); absent on old transcripts
            string? effort = LastString(tail, "\"effort\":\\s*\"([a-z]+)\"");
            string model = FriendlyModel(modelId);
            if (effort is { Length: > 0 }) model += " · " + (effort == "xhigh" ? "XHigh" : char.ToUpper(effort[0]) + effort[1..]);

            var goal = ReadPlan(file) ?? (cwd.Length > 0 ? ReadNextSession(cwd.Replace("\\\\", "\\")) : null);
            return new SessionContext(project, model, tokens, window, startedAt, active, state, goal);
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
    const int TailBytes = 128 * 1024;

    static (bool ended, bool asking, DateTimeOffset? ts) Classify(string tail, bool wholeFile)
    {
        bool? ended = null;
        bool asking = false;
        DateTimeOffset? ts = null;
        try
        {
            var lines = tail.Split('\n');
            int first = wholeFile ? 0 : 1; // a mid-file tail starts with a partial line
            for (int i = lines.Length - 1; i >= first && (ended is null || ts is null); i--)
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
                            ended = IsTurnEnd(type, root, out asking);
                    }
                }
            }
        }
        catch
        {
            return (false, false, null); // default to Working
        }
        return (ended ?? false, asking, ts);
    }

    static bool IsTurnEnd(string type, JsonElement root, out bool asking)
    {
        asking = false;
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
                asking = last.ValueKind == JsonValueKind.Object && last.TryGetProperty("type", out var bt) && bt.GetString() == "tool_use"
                    && last.TryGetProperty("name", out var nm) && nm.GetString() is "AskUserQuestion" or "ExitPlanMode";
                return asking;
            }
            return false;
        }

        // user: only an interrupt marker ends the turn
        if (!msg.TryGetProperty("content", out var uc)) return false;
        string? text = uc.ValueKind == JsonValueKind.String ? uc.GetString()
            : uc.ValueKind == JsonValueKind.Array && uc.GetArrayLength() > 0
              && uc[0].ValueKind == JsonValueKind.Object && uc[0].TryGetProperty("text", out var tx) && tx.ValueKind == JsonValueKind.String ? tx.GetString()
            : null;
        return text?.StartsWith("[Request interrupted", StringComparison.Ordinal) == true;
    }

    // ---- step list (goal progress) ----
    // The plan can sit anywhere in a transcript that grows to tens of MB, so each file is
    // read once and then only its new bytes; the step list is replayed from the tool calls.

    sealed class Plan
    {
        public long Pos;
        public readonly List<(string Text, string Status)> Steps = new();
    }

    static readonly Dictionary<string, Plan> Plans = new(StringComparer.OrdinalIgnoreCase);

    static Goal? ReadPlan(FileInfo file)
    {
        Plan plan;
        lock (Plans)
            if (!Plans.TryGetValue(file.FullName, out plan!)) Plans[file.FullName] = plan = new Plan();
        lock (plan)
        {
            if (file.Length == plan.Pos) return ToGoal(plan.Steps); // nothing new since last poll
            try
            {
                using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < plan.Pos) { plan.Pos = 0; plan.Steps.Clear(); } // rewritten
                fs.Seek(plan.Pos, SeekOrigin.Begin);
                // 1 MB chunks, so a 37 MB transcript's first read never holds the whole file;
                // only complete lines are consumed — a half-written last line waits for next time
                var buf = new byte[1 << 20];
                var line = new MemoryStream();
                int read;
                while ((read = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    int start = 0;
                    for (int nl; (nl = Array.IndexOf(buf, (byte)'\n', start, read - start)) >= 0; start = nl + 1)
                    {
                        line.Write(buf, start, nl - start);
                        plan.Pos += line.Length + 1;
                        if (line.Length > 0) ApplyIfPlanLine(plan.Steps, Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length));
                        line.SetLength(0);
                    }
                    line.Write(buf, start, read - start);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // goal is extra: a locked/rotating file must not drop the session row
            }
            return ToGoal(plan.Steps);
        }
    }

    static void ApplyIfPlanLine(List<(string Text, string Status)> steps, string line)
    {
        if (line.Contains("\"tool_use\"") && (line.Contains("update_plan") || line.Contains("wrap_up") || line.Contains("\"name\":\"Task")))
            ApplyPlanLine(steps, line);
    }

    static void ApplyPlanLine(List<(string Text, string Status)> steps, string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("isSidechain", out var sc) && sc.ValueKind == JsonValueKind.True) return;
            if (!root.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object
                || !msg.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) return;
            foreach (var b in content.EnumerateArray())
            {
                if (b.ValueKind != JsonValueKind.Object || Str(b, "type") != "tool_use"
                    || !b.TryGetProperty("input", out var input) || input.ValueKind != JsonValueKind.Object) continue;
                string name = Str(b, "name") ?? "";
                if (name.EndsWith("update_plan", StringComparison.Ordinal)) ApplyUpdatePlan(steps, input);
                else if (name.EndsWith("__wrap_up", StringComparison.Ordinal)) // the app checks the list off
                    for (int i = 0; i < steps.Count; i++) { if (steps[i].Status != "deleted") steps[i] = (steps[i].Text, "completed"); }
                else if (name == "TaskCreate")
                    steps.Add((Str(input, "activeForm") ?? Str(input, "subject") ?? "", "pending"));
                else if (name == "TaskUpdate" && int.TryParse(Str(input, "taskId"), out int id) && id >= 1 && id <= steps.Count
                         && Str(input, "status") is { } st)
                    steps[id - 1] = (steps[id - 1].Text, st);
            }
        }
        catch (JsonException) { } // a line we can't read just doesn't move the plan
    }

    /// <summary>items = new list; advance = finish the in_progress step, start the next pending; done:[n] / start:n are 1-based.</summary>
    static void ApplyUpdatePlan(List<(string Text, string Status)> steps, JsonElement input)
    {
        if (input.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            steps.Clear();
            foreach (var it in items.EnumerateArray())
                if (it.ValueKind == JsonValueKind.Object)
                    steps.Add((Str(it, "content") ?? "", Str(it, "status") ?? "pending"));
            return;
        }
        void Set(int i, string status) { if (i >= 0 && i < steps.Count) steps[i] = (steps[i].Text, status); }
        if (input.TryGetProperty("advance", out var adv) && adv.ValueKind == JsonValueKind.True)
        {
            Set(steps.FindIndex(s => s.Status == "in_progress"), "completed");
            Set(steps.FindIndex(s => s.Status == "pending"), "in_progress");
        }
        if (input.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.Array)
            foreach (var n in done.EnumerateArray())
                if (n.TryGetInt32(out int k)) Set(k - 1, "completed");
        if (input.TryGetProperty("start", out var start) && start.TryGetInt32(out int s1)) Set(s1 - 1, "in_progress");
    }

    static Goal? ToGoal(List<(string Text, string Status)> steps)
    {
        var live = steps.Where(s => s.Status != "deleted").ToList();
        // a finished list is history: hide it (and let an active NEXT-SESSION.md show instead)
        if (live.Count == 0 || live.All(s => s.Status == "completed")) return null;
        var current = live.FirstOrDefault(s => s.Status == "in_progress");
        if (current.Text is null) current = live.FirstOrDefault(s => s.Status != "completed");
        return new Goal(live.Count(s => s.Status == "completed"), live.Count, current.Text);
    }

    /// <summary>
    /// Fallback for sessions without a step list: the sprint brief NEXT-SESSION.md in the
    /// session's folder, when its "status: active" line says a sprint is running. Table rows
    /// with ⬜ are steps to do, rows with ✅ are done; the first ⬜ row's task cell names the step.
    /// </summary>
    static Goal? ReadNextSession(string cwd)
    {
        try
        {
            string path = Path.Combine(cwd, "NEXT-SESSION.md");
            if (!File.Exists(path)) return null;
            var lines = File.ReadAllLines(path);
            if (!lines.Take(15).Any(l => l.StartsWith("status: active", StringComparison.OrdinalIgnoreCase))) return null;
            int done = 0, total = 0;
            string? current = null;
            foreach (var l in lines.Where(l => l.TrimStart().StartsWith('|')))
            {
                if (l.Contains('⬜'))
                {
                    total++;
                    if (current is null)
                    {
                        var cells = l.Split('|').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
                        current = (cells.Length > 1 ? cells[1] : cells[0]).Replace("`", "").Replace("**", "");
                    }
                }
                else if (l.Contains('✅')) { total++; done++; }
            }
            return total > done ? new Goal(done, total, current) : null; // all ✅ = finished, hidden like a done task list
        }
        catch
        {
            return null; // locked mid-save etc.; the next poll tries again
        }
    }

    /// <summary>Runnable check, `--goal-selftest`: replays both tool styles, incrementally, from a temp transcript.</summary>
    public static void GoalSelfTest()
    {
        static string Call(string name, string input) =>
            "{\"type\":\"assistant\",\"message\":{\"content\":[{\"type\":\"tool_use\",\"name\":\"" + name + "\",\"input\":" + input + "}]}}\n";
        void Expect(Goal? got, Goal? want, string what)
        {
            if (got != want) throw new Exception($"{what}: got {got}, want {want}");
        }
        string path = Path.Combine(Path.GetTempPath(), $"goal-selftest-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, Call("mcp__ccd_turn__update_plan",
                "{\"items\":[{\"content\":\"A\",\"status\":\"in_progress\"},{\"content\":\"B\",\"status\":\"pending\"},{\"content\":\"C\",\"status\":\"pending\"}]}"));
            Expect(ReadPlan(new FileInfo(path)), new Goal(0, 3, "A"), "items");
            File.AppendAllText(path, Call("mcp__ccd_turn__update_plan", "{\"advance\":true}") + "{\"type\":\"assistant\",\"mess"); // half line
            Expect(ReadPlan(new FileInfo(path)), new Goal(1, 3, "B"), "advance");
            File.AppendAllText(path, "age\":{}}\n" + Call("mcp__ccd_turn__update_plan", "{\"done\":[2],\"start\":3}"));
            Expect(ReadPlan(new FileInfo(path)), new Goal(2, 3, "C"), "done/start");
            File.AppendAllText(path, Call("mcp__ccd_turn__wrap_up", "{\"recap\":\"x\"}"));
            Expect(ReadPlan(new FileInfo(path)), null, "wrap_up hides a finished list");

            File.Delete(path);
            path += "2";
            File.WriteAllText(path,Call("TaskCreate", "{\"subject\":\"s1\",\"activeForm\":\"Doing 1\"}")
                + Call("TaskCreate", "{\"subject\":\"s2\"}") + Call("TaskCreate", "{\"subject\":\"s3\"}")
                + Call("TaskUpdate", "{\"taskId\":\"1\",\"status\":\"completed\"}")
                + Call("TaskUpdate", "{\"taskId\":\"3\",\"status\":\"deleted\"}"));
            Expect(ReadPlan(new FileInfo(path)), new Goal(1, 2, "s2"), "tasks");
        }
        finally { File.Delete(path); }

        string dir = Directory.CreateTempSubdirectory("goal-selftest").FullName;
        try
        {
            string ns = Path.Combine(dir, "NEXT-SESSION.md");
            File.WriteAllText(ns, "# x\n\nstatus: active\n\n| Step | งาน | สถานะ |\n|---|---|---|\n| 1 | a | ✅ |\n| 2 | **b** `c` | ⬜ |\n| 3 | d | ⬜ |\n");
            Expect(ReadNextSession(dir), new Goal(1, 3, "b c"), "NEXT-SESSION active");
            File.WriteAllText(ns, File.ReadAllText(ns).Replace("status: active", "status: done"));
            Expect(ReadNextSession(dir), null, "NEXT-SESSION done");
        }
        finally { Directory.Delete(dir, true); }

        foreach (var s in GetActive(TimeSpan.FromHours(2), 12, "active"))
            Console.WriteLine($"{s.Project}: {s.Goal}");
        Console.WriteLine("goal selftest OK");
    }

    static string? Str(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

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
