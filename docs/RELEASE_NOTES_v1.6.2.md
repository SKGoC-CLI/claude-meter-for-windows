# Claude Meter v1.6.2

Hotfix: finds your Claude Desktop login again after Desktop's July 2026 update.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Fixed

- **The meter reads the Claude Desktop token again after Desktop switched to MSIX
  packaging.** The 2026-07-17 Desktop auto-update moved its data folder from
  `%APPDATA%\Claude` to `%LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude`
  and deleted the old one. The meter, still looking at the old path, lost the Desktop
  login and sat on **"Usage temporarily unavailable"** until the CLI was next used.
  It now checks both locations on every poll and follows whichever holds the freshest
  `config.json` — so it keeps working even if a future update moves the folder again
  mid-run. If your meter went amber today, this release fixes it.

Full history in [CHANGELOG.md](../CHANGELOG.md).
