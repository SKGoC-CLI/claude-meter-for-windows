# Claude Meter v1.12.0

Your purchased usage credits now show next to the cloud credit on one line, and each Claude Code session shows its progress as a short row of segments with the current step's headline. The tray menu is regrouped into four submenus.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/demo-v1.12.0.gif" width="640" alt="Claude Meter v1.12.0 demo — the popup above the Windows tray with the credit line Cloud credit $94.79 left · Usage credits $5.00 left, and under CURRENT SESSIONS (3) a session reading 7/7 release v1.12.0 beside a row of grey segments">
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/popup-v1.12.0.png" alt="Claude Meter v1.12.0 — dark-theme popup; the credit line under Monthly spend, and the first current session showing three of six step segments filled with 3/6 Draw the goal progress row">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **Usage credits balance.** The prepaid usage credits you bought appear as `Usage credits: $5.00 left`, read from the same endpoint Claude Code uses. When the read fails, the last known balance stays on screen and the meter retries after 30 minutes.
- **Step segments.** A session with up to 10 steps shows one short segment per step: done steps light grey, the current step white (it fades while the session is Working), the rest dark grey. They carry no colour, so they never read as the green or amber state dot. A dim `3/6` and the step's headline follow. Over 10 steps it falls back to `Step 3/14 · <step>`.

## Changed

- **One credit line.** Cloud credit and usage credits share one small line with no bars: `Cloud credit: $94.79 left · Usage credits: $5.00 left`, with the expiry on the right (amber within 7 days, red within 1 day).
- **The spend row is called Monthly spend**, matching claude.ai.
- **CURRENT SESSION(S).** The section is headed `CURRENT SESSION`, or `CURRENT SESSIONS (3)` with several, and its tray submenu is *Current session*.
- **Step names are headlines only.** Text after the first `(`, dash, `:`, `·` or `,` is dropped, and CLI task lists use each task's short subject.
- **Tray menu in four groups**: *Popup shows* (Limits / Current session / Graphs), *Window*, *Tray & alerts*, *App*.

## Upgrading

Unzip over your old installation folder. Settings, saved usage history, and login tokens are untouched.

Full history in [CHANGELOG.md](../CHANGELOG.md).
