# Claude Meter v1.4.0

The reliability polish update.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## New

- **Hotkey picker** — choose your combo (Ctrl+Alt+U default, plus Ctrl+Alt+C / Ctrl+Alt+M / Ctrl+Shift+U / Ctrl+Shift+M / Ctrl+U / Alt+U) or turn it Off. Handy when the default clashes with another app.

## Improved

- **Tray icon pinning** — choosing Session (5h) or Weekly in "Tray icon shows" now sticks to your choice. A maxed-out window (e.g. Fable Weekly at 99%) no longer forces the icon to show that instead. "Active limit (auto)" still warns at 90% so you don't miss a binding limit.

## Fixed

- "Start with Windows" opt-out now sticks instead of being silently re-enabled on every launch
- After you re-login in Claude Code, the meter auto-recovers instead of getting stuck on "login expired"
- The "Fix Claude login" button no longer breaks (or crashes) after changing the popup size
- Blank or unrecognized hotkey values self-heal to the default (Ctrl+Alt+U); null values no longer crash at startup
- Fixed duplicate "Extra usage" rows that could appear with older API response shapes
- The usage endpoint's rate-limit backoff now honors Retry-After headers given as HTTP dates
- The About dialog no longer leaks font handles when opened repeatedly
- Old log files are now cleaned up daily while the app runs

Full history in [CHANGELOG.md](../CHANGELOG.md).
