# Claude Meter v1.6.0

See every Claude Code session's context window at a glance.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## New

- **SESSION CONTEXT now shows every active session, not just one.** Run two or three
  Claude Code sessions at once and each gets its own row — bold session name, its
  percentage, and a `current / capacity` token readout (e.g. `169k / 1.0M`) so you can
  see exactly how full each context window is and how much room is left.
- **Two new options** (right-click the tray icon):
  - *Context: max shown* — how many session rows to display (1, 2, 3, or 5).
  - *Context: sort by* — last active, name (A–Z), or context fill (high → low).
- When your 5-hour window has reset to 0% and is idle, the Session row now reads
  **"resets 5h after next use"** instead of leaving the reset area blank — the next
  window (and its 5-hour clock) only begins the next time you use Claude.

## Fixed

- **The SESSION CONTEXT percentage was wrong on 1M-context accounts.** A session at 168k
  tokens on a 1M window was showing **84%** instead of the correct **17%** — the meter
  had guessed the window size from the token count. It now reads the size from the model
  (Opus/Sonnet → 1M, Haiku → 200k), matching what Claude itself reports.

Full history in [CHANGELOG.md](../CHANGELOG.md).
