# Claude Meter v1.1.0

Quality-of-life update focused on login recovery and diagnostics.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## What's new

- 🔑 **Fix Claude login** — when your login expires, the popup now shows a
  one-click button (and a tray menu item) that opens a terminal running the
  Claude CLI, ready for `/login`
- 🕐 Error view shows **"Last session at \<time\>"** so you know when data was
  last fetched
- 📁 **Local diagnostic logs** (`%APPDATA%\ClaudeMeter\logs`, activity + errors,
  never tokens, 7-day retention) + **Open log folder** menu — attach the latest
  log when reporting issues
- 🛡 Crashes are now captured to the log

## Fixed

- Footer countdown no longer overlaps error text
- Usage graph no longer draws on top of error messages

Full history in [CHANGELOG.md](../CHANGELOG.md).
