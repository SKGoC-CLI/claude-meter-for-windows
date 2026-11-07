# 02: All background task kinds, every ending, and expiry

**What to build:** Bash commands started with run_in_background and Agents launched in the background also make the session Background. A task also stops counting when it failed or was killed, when TaskStop names it, when a Monitor passes its own timeoutMs, or when any task without a timeout (Bash, Agent, persistent Monitor) is older than 3 hours — so a closed Claude app can't leave a row teal forever.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Bash background start = tool result with `backgroundTaskId`; Agent background start = tool result with `isAsync: true`, `status: async_launched`, `agentId`
- [ ] task-notification status failed / killed removes the task; `TaskStop` tool_use with `task_id` removes it
- [ ] Deadline = start + timeoutMs for Monitor (persistent → 3 h), start + 3 h otherwise; checked against now at each poll
- [ ] Selftest cases for each start kind, each ending, Monitor timeout, and the 3 h cap
