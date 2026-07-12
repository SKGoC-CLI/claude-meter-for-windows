# Changelog

All notable changes to Claude Usage Meter for Windows are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- Tray icon now shows the Session (5h) percentage by default instead of the
  highest window — new "Tray icon shows" menu (Session / Weekly / Highest);
  any window reaching 90 % still takes over the icon
- Token refresh now backs off politely when rate-limited: honors `Retry-After`,
  otherwise waits 10 minutes doubling up to a 2-hour cap, and retries
  immediately after a fresh login (previously it retried every 3 minutes)

## [1.1.0] - 2026-07-11

### Added
- **Fix Claude login** — when the login is broken, a tray menu item and an
  in-popup button open a terminal running the Claude CLI so you can `/login`
  in one click
- Error view shows **"Last session at \<time\>"** — the last successful data fetch
- **Local diagnostic logging**: daily files in `%APPDATA%\ClaudeMeter\logs`
  (activity + errors, never tokens), auto-pruned after 7 days
- **Open log folder** tray menu item for easy bug reports
- Global crash handlers write unhandled exceptions to the log
- Token-refresh failures now log the HTTP status and server response

### Changed
- Error view hides the usage graph and uses shorter messages

### Fixed
- Footer countdown no longer overlaps error text

## [1.0.1] - 2026-07-10

### Added
- Initial public release
- Live tray icon with color-coded usage percentage
- Dark/light themed popup: progress bars, reset countdown + actual reset time
- Usage-remaining graph with hourly time axis, "now" marker, reset markers,
  configurable range (24h/12h) and now-position (center / 3⁄4 / right)
- Always on top (pin + drag anywhere) with optional click-through
- 3 sizes, 5 opacity levels with hover-restore
- Usage alert notification at a configurable threshold (Off / 50–95 %)
- Global hotkey (Ctrl+Alt+U), autostart with Windows, single instance
- Automatic update check against GitHub Releases

[1.1.0]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.1.0
[1.0.1]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.0.1
