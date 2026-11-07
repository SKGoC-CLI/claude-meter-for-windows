# Spec: teal blinking dot for sessions with background tasks

Labels: ready-for-agent
Source: WORK-LOG-2026-11-08-background-dot.md (grilling 2026-11-08)

## Problem Statement

When a Claude Code session finishes its turn but has left a background task running (a Monitor
watching a log, a Bash command started in the background, an Agent launched in the background),
the CURRENT SESSIONS list shows the session as Waiting (amber) or Idle (grey). The user cannot
tell at a glance that work is still going on in that session. Worse, if the background task runs
quietly for more than 30 minutes, the session row disappears entirely (DropAfter), even though
the session will wake up when the task finishes.

## Solution

A new session state, **Background**, shown as a teal dot that blinks slowly (slower than the
green Working blink, so the two are easy to tell apart). It appears only when the turn has ended
and at least one background task is still live. Line 2 of the session block gets a short
`⟳ <task description>` (or `⟳ N bg: <first description>` for several). A session with live
background tasks is not dropped after 30 minutes; it stays until each task's own timeout, or
3 hours for tasks without one, after which the task is treated as finished.

State priority: Working (green, blinking) > Asking (amber, blinking) > **Background (teal,
blinking)** > Waiting (amber, steady) > Idle (grey, row dimmed).

## User Stories

1. As the user, I want a session whose turn ended but still has a background task to show a teal blinking dot, so that I know work is still happening there.
2. As the user, I want the teal blink to be slower than the green Working blink, so that I can tell "Claude is thinking" from "a task is running in the background" without reading.
3. As the user, I want a session that is actively working to keep its green dot even if it also has background tasks, so that the dot still tells me Claude is mid-turn.
4. As the user, I want a session that is asking me a question to keep its amber blinking dot even with background tasks, so that I never miss a question waiting for me.
5. As the user, I want the teal dot to turn back into Waiting/Idle as soon as the background task finishes, so that the dot doesn't lie.
6. As the user, I want a background task that completed, failed or was killed to stop counting, so that every ending is recognised.
7. As the user, I want a background task stopped with TaskStop to stop counting, so that a manually stopped task clears the dot.
8. As the user, I want a Monitor task to stop counting once its own timeout has passed, so that a Monitor that silently expired doesn't keep the dot teal.
9. As the user, I want a background task with no known timeout to stop counting after 3 hours, so that a crashed or closed Claude app can't leave a row teal forever.
10. As the user, I want a session with a live background task to stay in the list past the usual 30-minute drop, so that long quiet jobs (GPU queues, long builds) stay visible.
11. As the user, I want line 2 of the session block to show `⟳` plus the task's description, so that I know which job is running.
12. As the user, I want several background tasks to show as `⟳ N bg: <first>`, so that the line stays short.
13. As the user, I want a Background row to show the same step list / progress as a Waiting row, so that I don't lose that information.
14. As the user, I want the teal dot to read well in every theme (dark and light), so that it's visible whichever theme I use.
15. As the user, I want background work inside subagent transcripts ignored, so that the parent session alone owns the state (same as today).
16. As the user, I want the blink to respect the existing "alert speed" setting, so that it behaves like the other blinking dots.
17. As the user, I want the tray icon unchanged, so that the quota percentage stays readable.
18. As the user, I want detection to stay cheap (incremental reads), so that the meter doesn't slow down with 30+ MB transcripts.

## Implementation Decisions

- **Session states**: add `Background` to the session-state enum. The state decision becomes:
  turn not ended → Working; asking → Asking; live background tasks > 0 → Background;
  last seen within WaitingFor → Waiting; otherwise Idle.
- **Session context** gains background-task info: live count and the first task's description
  (nullable/zero when none).
- **Background-task tracking** reuses the incremental per-file reader already used for the step
  list (a remembered byte position per transcript; only new complete lines are parsed). It keeps
  a map task-id → (description, started-at, deadline). Ideally the same single pass handles both
  the plan and the tasks.
- **Start markers** (confirmed in real transcripts 2026-11-08), read from a tool result line,
  matched to its tool_use by tool_use_id to get the description:
  - Monitor: result `{"taskId": "...", "timeoutMs": N, "persistent": bool}` → deadline =
    start + timeoutMs (persistent → 3 h cap).
  - Bash with `run_in_background: true`: result has `"backgroundTaskId": "..."` → deadline = start + 3 h.
  - Agent in background: result `{"isAsync": true, "status": "async_launched", "agentId": "...", "description": ...}` → deadline = start + 3 h.
  - Description: the tool_use input `description` (Monitor/Bash/Agent all have one); fallback to the tool name.
- **End markers**: a `<task-notification>` in a user line with `<task-id>X</task-id>` and
  `<status>` in {completed, failed, killed} removes X; a `TaskStop` tool_use with `task_id: X`
  removes X; deadline passed (vs. now at poll time) removes X. Sidechain lines are ignored.
- **Drop rule**: a session is not dropped by DropAfter while it has a live background task; it
  falls back to the normal rule once all are gone (tasks expire at their deadline, so the cap is
  enforced by the deadline itself).
- **Theme**: add a teal theme colour (around `#2DD4BF` dark / `#0D9488` light, tuned for contrast
  in each theme).
- **Popup drawing**: Background dot uses the teal colour and the existing pulse helper at ~2 s per
  cycle × alert-speed multiplier, and registers its rect for the existing partial-repaint animation.
  Line 2 shows `⟳ <desc>` / `⟳ N bg: <desc>`, truncated to fit, alongside (or in place of an empty)
  step list. Background rows use the full block height (not the Idle one-line height).

## Testing Decisions

- One seam: the transcript reader. Tests write a fake transcript to a temp file, call the reader,
  and assert only on the resulting session's state and background info — never on internals.
- Cases: Monitor start → Background with its description; task-notification completed → back to
  Waiting; failed/killed likewise; TaskStop → cleared; Monitor past timeoutMs → cleared; Bash
  background past 3 h → cleared; two tasks → count 2 with first description; turn not ended with a
  live task → still Working; asking with a live task → still Asking; last activity 45 min ago with a
  live task → row present; half-written last line doesn't break the count.
- Prior art: the existing `--goal-selftest` (temp transcript, incremental appends, half-line case).
  Extend it or add a sibling `--bg-selftest` in the same style.
- UI (colour, blink rate, line-2 text) is checked by running the real app and taking screenshots in
  each theme for the user — no automated UI tests.

## Out of Scope

- Tray icon changes.
- Showing background tasks of cloud sessions or of sessions on other machines.
- Reading the tasks' output files or process lists to verify a task is really alive.
- Listing every background task's details (only count + first description).

## Further Notes

- Risk accepted by the user: if the Claude app is closed mid-task, no end notification is written;
  the row stays teal until the task's deadline (≤ 3 h).
- The exact JSON field names above came from transcripts of Claude Code 2.1.317; if a future
  version renames them, detection simply finds nothing (row falls back to Waiting/Idle) — fails safe.
