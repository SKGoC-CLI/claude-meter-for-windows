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
    Goal? Goal = null,
    int BgCount = 0,
    string? BgTask = null)
{
    public double Percent => Math.Clamp(Tokens * 100.0 / WindowSize, 0, 100);
}

enum SessionState { Working, Asking, Background, Waiting, Idle }

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

    /// <summary>A background task with no known timeout counts as live for this long; also how far back a quiet transcript is still looked at.</summary>
    public static readonly TimeSpan BackgroundCap = TimeSpan.FromHours(3);

    /// <summary>
    /// Active sessions (transcripts written within <paramref name="maxIdle"/> — a cheap
    /// LastWriteTime pre-filter, widened to BackgroundCap so a quiet session with a live background
    /// task is still seen; the real cut is DropAfter on the last transcript timestamp unless a task is live),
    /// sorted and capped to <paramref name="max"/>. sort: "active" (most-recent first) | "name" | "context".
    /// </summary>
    public static IReadOnlyList<SessionContext> GetActive(TimeSpan maxIdle, int max, string sort)
    {
        try
        {
            if (!Directory.Exists(ProjectsDir)) return Array.Empty<SessionContext>();

            var sessions = Directory.EnumerateFiles(ProjectsDir, "*.jsonl", SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .Where(f => DateTime.Now - f.LastWriteTime < (maxIdle > BackgroundCap ? maxIdle : BackgroundCap) && f.Length > 0)
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

    internal static SessionContext? ParseForTest(FileInfo file) => Parse(file);

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
            if (age > BackgroundCap) return null; // dead for longer than any task can live: skip the full read
            var (planGoal, bg) = SyncTranscript(file);
            // a live background task keeps an ended turn's row; a mid-turn one past DropAfter crashed and must go
            if (age > DropAfter && !(ended && bg.Count > 0)) return null;
            var state = !ended ? SessionState.Working : asking ? SessionState.Asking : bg.Count > 0 ? SessionState.Background
                : age <= WaitingFor ? SessionState.Waiting : SessionState.Idle;

            // effort lives on every user line ("effort":"medium"); absent on old transcripts
            string? effort = LastString(tail, "\"effort\":\\s*\"([a-z]+)\"");
            string model = FriendlyModel(modelId);
            if (effort is { Length: > 0 }) model += " · " + (effort == "xhigh" ? "XHigh" : char.ToUpper(effort[0]) + effort[1..]);

            var goal = planGoal ?? (cwd.Length > 0 ? ReadNextSession(cwd.Replace("\\\\", "\\")) : null);
            return new SessionContext(project, model, tokens, window, startedAt, active, state, goal, bg.Count, bg.Count > 0 ? bg[0].Desc : null);
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
        public readonly Dictionary<string, BgTask> Bg = new();                       // task id -> background task
        public readonly Dictionary<string, (string Name, string? Desc, long TimeoutMs)> Pending = new(); // tool_use id -> launch call
    }

    sealed record BgTask(string Desc, DateTimeOffset Started, DateTimeOffset Deadline);

    static readonly Dictionary<string, Plan> Plans = new(StringComparer.OrdinalIgnoreCase);

    static Goal? ReadPlan(FileInfo file) => SyncTranscript(file).Goal;

    /// <summary>Reads the file's new bytes, then returns the step list and the background tasks still live now (earliest-started first).</summary>
    static (Goal? Goal, List<BgTask> Live) SyncTranscript(FileInfo file)
    {
        Plan plan;
        lock (Plans)
            if (!Plans.TryGetValue(file.FullName, out plan!)) Plans[file.FullName] = plan = new Plan();
        lock (plan)
        {
            if (file.Length == plan.Pos) return Snapshot(plan); // nothing new since last poll
            try
            {
                using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < plan.Pos) { plan.Pos = 0; plan.Steps.Clear(); plan.Bg.Clear(); plan.Pending.Clear(); } // rewritten
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
                        if (line.Length > 0) ApplyIfPlanLine(plan, Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length));
                        line.SetLength(0);
                    }
                    line.Write(buf, start, read - start);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // goal is extra: a locked/rotating file must not drop the session row
            }
            return Snapshot(plan);
        }
    }

    static (Goal?, List<BgTask>) Snapshot(Plan plan)
    {
        var now = DateTimeOffset.Now; // expiry is judged at poll time, not parse time
        return (ToGoal(plan.Steps), plan.Bg.Values.Where(t => t.Deadline > now).OrderBy(t => t.Started).ToList());
    }

    static readonly string[] BgMarkers = { "\"taskId\"", "\"backgroundTaskId\"", "async_launched", "task-notification", "TaskStop", "KillShell" };
    // id and status must come from the same notification: never skip past its closing tag
    static readonly Regex NotificationRx = new(@"<task-id>\s*([^<\s]+)\s*</task-id>(?:(?!</task-notification>)[\s\S])*?<status>\s*(completed|failed|killed)\s*</status>", RegexOptions.Compiled);

    static void ApplyIfPlanLine(Plan plan, string line)
    {
        // cheap substring prefilter: most lines of a 30 MB transcript are neither plan nor background-task lines
        bool tool = line.Contains("\"tool_use\"");
        bool planLine = tool && (line.Contains("update_plan") || line.Contains("wrap_up") || line.Contains("\"name\":\"Task"));
        bool launchLine = tool && (line.Contains("\"name\":\"Monitor\"")
            || (line.Contains("\"run_in_background\":true") && (line.Contains("\"name\":\"Bash\"") || line.Contains("\"name\":\"Agent\""))));
        bool bgLine = false;
        foreach (var m in BgMarkers) if (line.Contains(m)) { bgLine = true; break; }
        if (!planLine && !launchLine && !bgLine) return;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;
            if (root.TryGetProperty("isSidechain", out var sc) && sc.ValueKind == JsonValueKind.True) return;
            if (planLine) ApplyPlanLine(plan.Steps, root);
            if (launchLine || bgLine) ApplyBgLine(plan, root);
        }
        catch (JsonException) { } // a line we can't read just doesn't move the plan
    }

    /// <summary>Background tasks: launch call (assistant) + its result (user) start one; a task-notification or TaskStop ends it.</summary>
    static void ApplyBgLine(Plan plan, JsonElement root)
    {
        if (!root.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object
            || !msg.TryGetProperty("content", out var content)) return;
        string? type = Str(root, "type");

        if (type == "assistant" && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var b in content.EnumerateArray())
            {
                if (b.ValueKind != JsonValueKind.Object || Str(b, "type") != "tool_use"
                    || !b.TryGetProperty("input", out var input) || input.ValueKind != JsonValueKind.Object) continue;
                string name = Str(b, "name") ?? "";
                if (name is "TaskStop" or "KillShell")
                {
                    if ((Str(input, "task_id") ?? Str(input, "shell_id")) is { } sid) plan.Bg.Remove(sid);
                    continue;
                }
                bool bg = name == "Monitor"
                    || (name is "Bash" or "Agent" && input.TryGetProperty("run_in_background", out var rb) && rb.ValueKind == JsonValueKind.True);
                if (!bg || Str(b, "id") is not { } id) continue;
                if (plan.Pending.Count > 200) plan.Pending.Clear(); // launches whose result never came
                plan.Pending[id] = (name, Str(input, "description"), input.TryGetProperty("timeout_ms", out var to) && to.TryGetInt64(out long ms) ? ms : 0);
            }
            return;
        }
        if (type != "user") return;

        // end: <task-notification> in the user line's text
        var texts = new List<string>();
        if (content.ValueKind == JsonValueKind.String) texts.Add(content.GetString() ?? "");
        else if (content.ValueKind == JsonValueKind.Array)
            foreach (var b in content.EnumerateArray())
                if (b.ValueKind == JsonValueKind.Object && Str(b, "text") is { } tx) texts.Add(tx);
        foreach (var t in texts)
            if (t.Contains("task-notification"))
                foreach (Match m in NotificationRx.Matches(t)) plan.Bg.Remove(m.Groups[1].Value);

        // start: the launch call's tool result
        if (!root.TryGetProperty("toolUseResult", out var tur) || tur.ValueKind != JsonValueKind.Object) return;
        (string Name, string? Desc, long TimeoutMs) call = default;
        if (content.ValueKind == JsonValueKind.Array)
            foreach (var b in content.EnumerateArray())
                if (b.ValueKind == JsonValueKind.Object && Str(b, "tool_use_id") is { } tid && plan.Pending.Remove(tid, out call)) break;

        string? taskId = null;
        long timeoutMs = 0;
        bool persistent = tur.TryGetProperty("persistent", out var pe) && pe.ValueKind == JsonValueKind.True;
        string fallback = call.Name ?? "";
        if (Str(tur, "backgroundTaskId") is { } bid) { taskId = bid; if (fallback.Length == 0) fallback = "Bash"; }
        else if (Str(tur, "status") == "async_launched" && Str(tur, "agentId") is { } aid) { taskId = aid; if (fallback.Length == 0) fallback = "Agent"; }
        else if (Str(tur, "taskId") is { } mid && (call.Name == "Monitor" || tur.TryGetProperty("timeoutMs", out _))) // other tools' results carry a taskId too
        {
            taskId = mid;
            if (fallback.Length == 0) fallback = "Monitor";
            timeoutMs = tur.TryGetProperty("timeoutMs", out var tm) && tm.TryGetInt64(out long v) ? v : call.TimeoutMs;
        }
        if (taskId is null) return;

        var started = root.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(ts.GetString(), out var parsed) ? parsed : DateTimeOffset.Now;
        var deadline = started + (timeoutMs > 0 && !persistent ? TimeSpan.FromMilliseconds(Math.Min(timeoutMs, BackgroundCap.TotalMilliseconds)) : BackgroundCap);
        string desc = Regex.Replace(call.Desc ?? Str(tur, "description") ?? fallback, @"\s+", " ").Trim(); // one line in the popup
        plan.Bg[taskId] = new BgTask(string.IsNullOrWhiteSpace(desc) ? fallback : desc, started, deadline);
    }

    static void ApplyPlanLine(List<(string Text, string Status)> steps, JsonElement root)
    {
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
                steps.Add((Str(input, "subject") ?? Str(input, "activeForm") ?? "", "pending"));
            else if (name == "TaskUpdate" && int.TryParse(Str(input, "taskId"), out int id) && id >= 1 && id <= steps.Count
                     && Str(input, "status") is { } st)
                steps[id - 1] = (steps[id - 1].Text, st);
        }
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
        return new Goal(live.Count(s => s.Status == "completed"), live.Count, ShortStep(current.Text));
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
            return total > done ? new Goal(done, total, current is null ? null : ShortStep(current)) : null; // all ✅ = finished, hidden like a done task list
        }
        catch
        {
            return null; // locked mid-save etc.; the next poll tries again
        }
    }

    static readonly string[] StepSeparators = { " (", "(", " —", " – ", ": ", " · ", ", " };

    /// <summary>The step's headline: text before the first "(", "—", ":", "·" or ",", so a
    /// "release v1.12.0 (incl. X) — wait for go" cell shows as "release v1.12.0".</summary>
    static string ShortStep(string text)
    {
        text = text.Trim();
        int cut = StepSeparators.Select(s => text.IndexOf(s, StringComparison.Ordinal)).Where(i => i > 0).DefaultIfEmpty(-1).Min();
        string head = cut > 0 ? text[..cut].TrimEnd() : text;
        return head.Length > 0 ? head : text;
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
                + Call("TaskCreate", "{\"subject\":\"s2\",\"activeForm\":\"Doing 2\"}") + Call("TaskCreate", "{\"subject\":\"s3\"}")
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
            if (ShortStep("release v1.12.0 (incl. x) — wait") != "release v1.12.0" || ShortStep("tweak: y") != "tweak"
                || ShortStep("plain step") != "plain step") throw new Exception("ShortStep");
        }
        finally { Directory.Delete(dir, true); }

        foreach (var s in GetActive(TimeSpan.FromHours(2), 12, "active"))
            Console.WriteLine($"{s.Project}: {s.Goal}");
        Console.WriteLine("goal selftest OK");
    }

    /// <summary>Runnable check, `--bg-selftest`: background-task tracking through the real parse path.</summary>
    public static void BgSelfTest()
    {
        var dir = Directory.CreateTempSubdirectory("bg-selftest").FullName;
        int n = 0;
        string Iso(TimeSpan ago) => (DateTimeOffset.UtcNow - ago).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string Tail(string ts) => "\"timestamp\":\"" + ts + "\",\"cwd\":\"C:\\\\x\\\\proj\"";
        string Use(string ts, string name, string input, string id) =>
            "{\"type\":\"assistant\"," + Tail(ts) + ",\"message\":{\"model\":\"claude-opus-4-7\",\"stop_reason\":\"tool_use\",\"usage\":{\"input_tokens\":100,\"cache_read_input_tokens\":5000},\"content\":[{\"type\":\"tool_use\",\"id\":\"" + id + "\",\"name\":\"" + name + "\",\"input\":" + input + "}]}}\n";
        string Res(string ts, string id, string tur) =>
            "{\"type\":\"user\"," + Tail(ts) + ",\"message\":{\"content\":[{\"type\":\"tool_result\",\"tool_use_id\":\"" + id + "\",\"content\":\"ok\"}]},\"toolUseResult\":" + tur + "}\n";
        string End(string ts) =>
            "{\"type\":\"assistant\"," + Tail(ts) + ",\"message\":{\"model\":\"claude-opus-4-7\",\"stop_reason\":\"end_turn\",\"usage\":{\"input_tokens\":100,\"cache_read_input_tokens\":5000},\"content\":[{\"type\":\"text\",\"text\":\"done\"}]}}\n";
        string Note(string ts, string id, string status) =>
            "{\"type\":\"user\"," + Tail(ts) + ",\"message\":{\"role\":\"user\",\"content\":\"<task-notification>\\n<task-id>" + id + "</task-id>\\n<status>" + status + "</status>\\n</task-notification>\"}}\n";
        string Mon(string ts, string tid, string id, string desc, long timeoutMs) =>
            Use(ts, "Monitor", "{\"command\":\"tail\",\"description\":\"" + desc + "\",\"timeout_ms\":" + timeoutMs + "}", tid)
            + Res(ts, tid, "{\"taskId\":\"" + id + "\",\"timeoutMs\":" + timeoutMs + ",\"persistent\":false}");
        string Bash(string ts, string tid, string id, string desc) =>
            Use(ts, "Bash", "{\"command\":\"sleep\",\"run_in_background\":true,\"description\":\"" + desc + "\"}", tid)
            + Res(ts, tid, "{\"stdout\":\"\",\"stderr\":\"\",\"interrupted\":false,\"backgroundTaskId\":\"" + id + "\"}");

        SessionContext? Run(string text, string? append = null)
        {
            string path = Path.Combine(dir, $"s{n++}.jsonl");
            File.WriteAllText(path, text);
            if (append is not null) { ParseForTest(new FileInfo(path)); File.AppendAllText(path, append); } // second poll reads only the new bytes
            return ParseForTest(new FileInfo(path));
        }
        void Expect(SessionContext? c, SessionState? state, int count, string? task, string what)
        {
            if (state is null) { if (c is not null) throw new Exception($"{what}: got {c.State}, want dropped"); return; }
            if (c is null || c.State != state || c.BgCount != count || c.BgTask != task)
                throw new Exception($"{what}: got {(c is null ? "null" : $"{c.State}/{c.BgCount}/{c.BgTask}")}, want {state}/{count}/{task}");
        }
        try
        {
            var t1 = Iso(TimeSpan.FromMinutes(5));
            var t2 = Iso(TimeSpan.FromMinutes(4));
            string start = Mon(t1, "toolu_1", "mon1", "queue.log: errors", 600_000 * 6) + End(t2);
            Expect(Run(start), SessionState.Background, 1, "queue.log: errors", "monitor + end_turn");
            foreach (var st in new[] { "completed", "failed", "killed" })
                Expect(Run(start, Note(Iso(TimeSpan.FromMinutes(2)), "mon1", st) + End(Iso(TimeSpan.FromMinutes(1)))), SessionState.Waiting, 0, null, "notification " + st);
            Expect(Run(start, Use(Iso(TimeSpan.FromMinutes(1)), "TaskStop", "{\"task_id\":\"mon1\"}", "toolu_s") + End(Iso(TimeSpan.FromMinutes(1)))),
                SessionState.Waiting, 0, null, "TaskStop");
            Expect(Run(Mon(Iso(TimeSpan.FromMinutes(20)), "toolu_1", "mon1", "old", 600_000) + End(Iso(TimeSpan.FromMinutes(19)))),
                SessionState.Idle, 0, null, "monitor timeout elapsed");
            Expect(Run(Bash(Iso(TimeSpan.FromHours(4)), "toolu_1", "bg1", "old bash") + End(Iso(TimeSpan.FromMinutes(5)))),
                SessionState.Waiting, 0, null, "bash 4h old");
            Expect(Run(Bash(Iso(TimeSpan.FromMinutes(9)), "toolu_1", "bg1", "first") + Bash(Iso(TimeSpan.FromMinutes(8)), "toolu_2", "bg2", "second") + End(t2)),
                SessionState.Background, 2, "first", "two tasks");
            string agent = Use(Iso(TimeSpan.FromMinutes(9)), "Agent", "{\"description\":\"wire pose\",\"run_in_background\":true}", "toolu_a")
                + Res(Iso(TimeSpan.FromMinutes(9)), "toolu_a", "{\"isAsync\":true,\"status\":\"async_launched\",\"agentId\":\"a97\",\"description\":\"wire pose\"}");
            Expect(Run(agent + End(t2)), SessionState.Background, 1, "wire pose", "agent");
            Expect(Run(start.Replace("\"stop_reason\":\"end_turn\"", "\"stop_reason\":\"tool_use\"")), SessionState.Working, 1, "queue.log: errors", "turn not ended");
            Expect(Run(start + Use(Iso(TimeSpan.FromMinutes(3)), "AskUserQuestion", "{\"questions\":[]}", "toolu_q")),
                SessionState.Asking, 1, "queue.log: errors", "asking");

            var q1 = Iso(TimeSpan.FromMinutes(45));
            string quiet = Mon(q1, "toolu_1", "mon1", "gpu queue", 3 * 3_600_000L) + End(Iso(TimeSpan.FromMinutes(44)));
            Expect(Run(quiet), SessionState.Background, 1, "gpu queue", "45 min quiet");
            Expect(Run(quiet, Note(Iso(TimeSpan.FromMinutes(41)), "mon1", "completed") + End(Iso(TimeSpan.FromMinutes(40)))), null, 0, null, "45 min quiet + done");
            Expect(Run(start, "{\"type\":\"user\",\"message\":{\"content\":\"<task-notif"), SessionState.Background, 1, "queue.log: errors", "half-written last line");
            // crashed mid-turn with a live task: still dropped after DropAfter, as before
            Expect(Run(Mon(Iso(TimeSpan.FromMinutes(45)), "toolu_1", "mon1", "gpu queue", 3 * 3_600_000L)), null, 0, null, "crashed mid-turn");
            Expect(Run(start, Use(Iso(TimeSpan.FromMinutes(1)), "KillShell", "{\"shell_id\":\"mon1\"}", "toolu_k") + End(Iso(TimeSpan.FromMinutes(1)))),
                SessionState.Waiting, 0, null, "KillShell");
            // a notification without status must not borrow the next one's
            Expect(Run(start, "{\"type\":\"user\"," + Tail(Iso(TimeSpan.FromMinutes(2))) + ",\"message\":{\"content\":\"<task-notification><task-id>mon1</task-id></task-notification><task-notification><task-id>zz</task-id><status>completed</status></task-notification>\"}}\n"
                + End(Iso(TimeSpan.FromMinutes(1)))), SessionState.Background, 1, "queue.log: errors", "status-less notification");
        }
        finally { Directory.Delete(dir, true); }
        Console.WriteLine("bg selftest OK");
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
