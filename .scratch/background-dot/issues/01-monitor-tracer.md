# 01: Monitor tracer — teal blinking dot while a Monitor runs

**What to build:** When a session's turn has ended but a Monitor it started is still live, the session row in CURRENT SESSIONS shows a new Background state: a teal dot blinking at ~2 s per cycle (× alert-speed multiplier). When the transcript records a task-notification with status completed for that task id, the row falls back to Waiting/Idle. Working and Asking still win over Background. Spec: `.scratch/background-dot/spec.md`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Session-state enum has Background; state priority is Working > Asking > Background > Waiting > Idle
- [ ] Background tasks are tracked incrementally per transcript (only new complete lines parsed; half-written last line waits), sidechain lines ignored
- [ ] Monitor start = tool result with `taskId` (+ `timeoutMs`), description taken from the matching tool_use input `description`
- [ ] `<task-notification>` with `<task-id>X</task-id>` and `<status>completed</status>` removes X
- [ ] New teal theme colour for dark and light themes; Background dot uses it, blinks via the existing pulse + partial-repaint animation; Background rows use the full block height
- [ ] Selftest (extend `--goal-selftest` or add `--bg-selftest`) covers: Monitor start → Background; completed → Waiting; live task while turn not ended → Working; live task while asking → Asking
- [ ] Build passes; selftest passes
