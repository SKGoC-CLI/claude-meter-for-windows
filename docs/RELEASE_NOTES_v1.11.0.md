# Claude Meter v1.11.0

Each Claude Code session now shows how far it is through its task, as `Step 3/6 · <current step>`. Idle sessions shrink to one line, so the popup stays compact with several sessions open.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/demo-v1.11.0.gif" width="640" alt="Claude Meter v1.11.0 demo — SESSION CONTEXT with a session showing Step 1/3 on its second line, state dots pulsing, and an idle session on a single line">
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/popup-v1.11.0.png" alt="Claude Meter v1.11.0 — dark-theme popup; the first session reads Step 3/6 with the session age on the right, and the idle session sits on one line">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **Step progress for each Claude Code session.** While a session works through a multi-step task, its second line in SESSION CONTEXT reads `Step 3/6 · <current step>`. The steps come from the session's task list in its transcript: the Claude desktop app's plan, or `TaskCreate` / `TaskUpdate` in the CLI. A session with no task list falls back to the sprint table in `NEXT-SESSION.md` in its folder, counting ✅ and ⬜ rows, but only while that file has a `status: active` line. A finished list is hidden. Long step names are cut with an ellipsis.

- **Show step progress toggle.** Tray menu → *Session context* → *Show step progress*. On by default.

## Changed

- **Session age is right-aligned on every row.** It sits at the right of the second line, under the token count. Step progress or the distance to auto-compact takes the left side.
- **Idle sessions take a single line**: name, model, context % and `tokens · age`, with no bar.
- **Transcripts are read incrementally.** Each poll reads only the bytes added since the last one, in 1 MB chunks. A large transcript is never loaded into memory at once.

## Upgrading

Unzip over your old installation folder. Settings, saved usage history, and login tokens are untouched. Show step progress defaults to on.

Full history in [CHANGELOG.md](../CHANGELOG.md).
