# Changelog

All notable changes to Claude Usage Meter for Windows are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.4.0] - 2026-07-12

### Changed
- The Hotkey menu is now a picker: choose between Ctrl+Alt+U (default),
  Ctrl+Alt+C, Ctrl+Alt+M, Ctrl+Shift+U, Ctrl+Shift+M, Ctrl+U, Alt+U or Off —
  handy when the default combo clashes with another app (note: single-modifier
  combos like Ctrl+U take that key over system-wide, e.g. underline in editors)
- Tray icon now honors an explicit "Tray icon shows" pin: choosing Session (5h)
  or Weekly always shows that window, instead of a near-maxed window
  (e.g. Fable Weekly at 99%) taking over the icon. "Active limit (auto)" still
  switches to any window that reaches 90% so a binding limit isn't missed

### Fixed
- "Start with Windows" is no longer silently re-enabled on every launch —
  turning it off now sticks (it was being re-applied as a first-run default
  each time the app started)
- After re-running Claude Code's login, the meter now recovers automatically
  instead of staying stuck on "login expired": it no longer clings to a stale
  cached refresh token and will use the newer one
- The "Fix Claude login" button no longer renders broken (or throws) after
  changing the popup Size, which previously left it holding a disposed font
- A blank or unrecognized hotkey value in settings is now self-healed back to
  the default (Ctrl+Alt+U) instead of leaving the hotkey silently inactive;
  a null hotkey value also no longer crashes the app at startup
- Fixed a duplicate "Extra usage" row that could appear when the usage endpoint
  returned the older response shape
- The usage endpoint's rate-limit backoff now also honors a Retry-After header
  given as an HTTP date, not just a delay in seconds
- The About dialog no longer leaks a few font handles each time it's opened
- Old log files are now cleaned up daily while the app runs, not only at startup

## [1.3.0] - 2026-07-12

### Added
- **Session context section**: shows how full the active Claude Code session's
  context window is (read locally from the session transcript — no network),
  with project name, model, tokens until auto-compact and session age;
  toggleable via "Show session context"
- **Show limits menu**: tick/untick each usage limit row individually — hidden
  rows still count for the tray icon and alerts

### Changed
- Popup reorganized into clearly divided sections (limits / session context /
  session graph) with matching small-caps headers

## [1.2.0] - 2026-07-12

### Fixed
- Model-scoped weekly limits (e.g. "Fable Weekly") disappeared after Anthropic
  moved them into the new `limits` array — the parser now reads that shape
  first, with the legacy fields as fallback

### Added
- "Tray icon shows" gains **Active limit (auto)** — the new default trusts the
  server's flag for whichever limit is currently binding
- Extra-usage credits and spend are shown as extra rows when enabled on the
  account

### Changed
- Graph "Now" marker shows a live hh:mm:ss clock; graph title renamed to
  "Session Graph" to reflect what it plots
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

[1.3.0]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.3.0
[1.2.0]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.2.0
[1.1.0]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.1.0
[1.0.1]: https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.0.1
