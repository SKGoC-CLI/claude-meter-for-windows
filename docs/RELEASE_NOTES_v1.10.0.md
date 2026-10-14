# Claude Meter v1.10.0

Each Claude Code session now shows whether it is working, waiting, or asking you a question, the tray pulses while a session is busy, and a cloud credit row shows what is left of your cloud-session credit.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/demo-v1.10.0.gif" width="640" alt="Claude Meter v1.10.0 demo — two Claude Code sessions labelled Opus · Medium, their state dots changing from amber (Waiting) to a pulsing green (Working)">
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/popup-v1.10.0.png" alt="Claude Meter v1.10.0 — dark-theme popup with three Claude Code sessions, each with a state dot and a model label such as Opus · High, and the Cloud credit row under the limits">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **Each Claude Code session shows a state dot in SESSION CONTEXT.** The state comes from the timestamp on the last line of the session's transcript, not the file's modified time. **Green** = Working (Claude is still answering or running a tool). **Amber** = Waiting (Claude has finished its turn, within the last 10 minutes). **Grey** = Idle (older than 10 minutes; the row is dimmed). Subagent transcripts are not listed.

- **Asking state for pending questions.** While Claude waits on an answer to `AskUserQuestion` or `ExitPlanMode`, the session is Asking: its dot pulses amber in the popup, and the tray background pulses orange, which takes priority over Working. Permission prompts are not written to the transcript, so they still show as Working.

- **Session alerts submenu.** Tray menu → *Session context* → *Session alerts*:
  - **Speed** — Slow, Normal or Fast. Scales the period of every pulse in the popup and the tray.
  - **Pulse tray while Working** — dims the tray digits to about 45% and back, every 2 seconds (Normal), while any session is Working.
  - **Pulse tray orange while Asking** — fades the tray background orange and back, every 1 second (Normal), while any session is Asking.

  Both toggles are on by default.

- **Effort level next to the model.** SESSION CONTEXT reads the effort level from the transcript and shows it after the model, e.g. `Opus · High`. Transcripts without an effort field show the model alone.

- **Cloud credit row.** When the usage endpoint returns cloud-session credit, Show limits gets a **Cloud credit** row: `$98.51 left`, a bar for the share used, and `expires in 33d (17 Nov)`. It is a wallet, not a limit, so it never drives the tray icon, usage alerts, the burn-rate ETA or the credit graph. Like the other rows, it can be hidden from Show limits.

## Changed

- **Session context refreshes every 2 seconds** instead of 5, so a short question is caught while it is pending.
- **Sessions stay listed for 30 minutes after their last activity** (was 10). They are greyed as Idle after 10 minutes.
- **The session scan runs off the UI thread**, and the popup ignores a session refresh that changes nothing, so it no longer re-lays out every few seconds.

## Upgrading

Unzip over your old installation folder. Settings, saved usage history, and login tokens are untouched. The new Session alerts options default to on, at Normal speed.

Full history in [CHANGELOG.md](../CHANGELOG.md).
