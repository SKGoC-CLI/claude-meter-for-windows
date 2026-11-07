# 03: Keep a Background row past the 30-minute drop

**What to build:** A session with at least one live background task stays in CURRENT SESSIONS even if its transcript has been quiet longer than DropAfter (30 min). Once all its tasks end or expire, the normal drop rule applies again. The cheap LastWriteTime pre-filter must not hide such sessions either (within the 3 h cap).

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Session last active 45 min ago with a live task → present, Background
- [ ] Same session after the task ends → dropped as before
- [ ] Pre-filter window covers at least the 3 h cap for sessions with live tasks (or tracked task state bypasses it) without parsing many more files per poll
- [ ] Selftest case for both of the above
