# Claude Meter v1.0.1 — first public release 🎉

Windows system-tray monitor for Claude usage limits (Session 5h / Weekly /
per-model weekly) — the same numbers as Claude Code's `/usage`, always visible.

## Download

**`ClaudeMeter-portable.zip`** (attached below) — unzip and run `ClaudeMeter.exe`.
Single self-contained exe, no .NET installation needed. See `README.txt` inside
for setup steps.

> SmartScreen warning on first run: **More info → Run anyway** (unsigned exe).

## Requirements

- Windows 10/11 64-bit
- Claude Code CLI logged in once (`claude` → `/login`)

## Highlights

- Live tray icon with color-coded usage percentage
- Dark popup: progress bars, reset countdown + actual reset time
- Always on top (pin + drag anywhere) with optional click-through
- 3 sizes, 5 opacity levels (hover restores full opacity)
- Usage alert notification at a configurable threshold (50–95 %)
- Optional 24-hour history sparkline
- Live countdown to next refresh; all settings persist
