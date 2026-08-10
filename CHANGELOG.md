# Changelog

All notable changes to Claude Usage Meter for Windows are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.9.1] - 2026-08-10

### Changed
- **The light theme was rebuilt around a light background instead of an inverted dark one.**
  Its status colours had been carried over from the dark theme, where they sit on `#1e1e1e`;
  on the pale popup background they washed out badly — the percentage number, the single most
  important thing in the window, ran at 2.4:1 contrast against a 4.5:1 requirement, and the
  amber 70–89% state at 1.7:1. Light mode now uses the Windows 11 Fluent semantic colours
  (`#005FB8` / `#9D5D00` / `#C42B1C`, all above 4.5:1), a near-white `#fbfbfb` surface in place
  of the flat `#f4f4f4` grey that read as a disabled panel, a progress-bar track light enough
  to be a groove rather than a smear, and darker secondary text for the many 7–8 pt labels.
  The reset markers on the session graph got a light-mode green too. **Dark mode is unchanged
  down to the byte** — the new theme-aware colours return the previous dark values verbatim.
- **The popup has a visible edge in light mode.** A `#fbfbfb` window on top of a white one had
  no discernible boundary. The popup now tints its DWM window border, which keeps the rounded
  corners Windows already draws; dark mode keeps the system default border.
- **The tray icon keeps its own colours.** The popup's status colours and the tray icon's are
  now separate. The icon sits on the taskbar, which follows the *Windows* theme rather than
  this app's setting, so darkening it for a light popup would have hidden the digits on a dark
  taskbar. The tray icon is untouched by this release.
- **The logo dropped its dark tile.** The header and About-dialog logo was a rounded black
  square, which turned into a solid dark block on a light background. It is now the ring and
  chick on transparency, so one file works on both themes. The app icon on the desktop and
  taskbar still has its tile.

## [1.9.0] - 2026-08-09

### Added
- **A minimize button in the popup's top-right corner.** Click "−" to hide the popup
  without reaching for the tray icon or the Esc key. It hides the window only — your
  *Always on top* setting is untouched, so a pinned popup comes back pinned, in the
  same place. The button is always there, including while pinned, where the popup
  otherwise has no visible way to dismiss it by mouse.
- **Mini mode collapses the popup to a single bar.** Tray menu → **Mini mode** shrinks
  the whole popup to one line reading `5h 29% · W 34%`, each number coloured by its own
  severity, sized to fit its text. It is for people who want the numbers on screen all
  day without giving up the space. Turning it on pins the popup automatically — a bar
  that disappears every time you click elsewhere would be useless — and turning it off
  restores your previous pin setting. The weekly number is the highest of *all* your
  weekly windows, the same rule the two-row tray icon uses, and it ignores *Show limits*
  so hiding a row in the full popup never blanks the bar.
- **Burn-rate ETA under each limit.** A small line under each progress bar estimates how
  long until that limit fills at your current pace — "full in ~2h 41m" — answering the
  question the percentage alone can't: how much longer can I keep working? Toggle it with
  **Show ETA** at the bottom of the *Show limits* menu. The pace is measured over a window
  matched to the limit's own length: one hour for the 5-hour session, 24 hours for weekly
  limits, because an hour of heavy use extrapolated across a week predicts a wall that a
  night's sleep erases. The estimate is deliberately quiet — it stays hidden when you are
  idle, when there is less than 10 minutes of history to measure, and whenever the limit
  would reset before it ever fills.

## [1.8.0] - 2026-08-05

### Added
- **Two-row tray icon mode shows Session (5h) and your highest weekly limit stacked.**
  Tray menu → *Tray icon shows* → new option **Session + Weekly**. The top row is
  always Session (5h); the bottom row is the highest of *all* your weekly windows —
  the plain all-models weekly, or a model-scoped one such as Opus Weekly when that is
  the one closer to binding. Each row is coloured by its own
  severity: blue <70%, orange 70–89%, red 90%+. The mode solves a real blind spot —
  after a weekly reset, heavy usage can drain the week to 80% while the 5-hour session
  reads 0%, and the existing auto mode only switches to a second window at 90%, leaving
  the week silently maxing out. The mode is opt-in; the default stays *Active limit
  (auto)*, so existing icons don't change on upgrade. Implementation note: two numbers
  are illegible when the icon is downscaled to 16 px, so this mode uses a hand-rolled
  bitmap font drawn directly at the final size instead of Segoe UI.

## [1.7.1] - 2026-07-29

### Added
- **Short credit-graph ranges.** Tray menu → *Credit graph* → *Range* now offers
  **24 hours** and **3 days** alongside 7 / 15 / 30 days. Those two ranges switch
  the time axis to hourly ticks — labelled every 3 h on the 24-hour view and every
  12 h on the 3-day view, with the date shown at midnight — because a day-scale
  axis left the 24-hour view with a single gridline. The Y axis still runs
  $0 → monthly limit on every range, so short views read as a nearly flat line by
  design; what they buy is time resolution, not vertical detail.
- **Month-reset marker.** Credit spend is cumulative *within a month*, so on the
  1st the series drops from last month's total to $0. A dashed red vertical line
  labelled "reset" now marks that boundary on every range, instead of leaving an
  unexplained cliff.

### Fixed
- **Tray icon showed "99" at 100%.** `IconRenderer` clamped the number with
  `Math.Min(99, …)` because three digits overflowed the 32 px icon, so a maxed-out
  Session (5h) window read as 99 % while the popup and tooltip correctly said
  100 %. The number is no longer clamped; when the text overflows it is squeezed
  horizontally instead of shrunk, keeping glyph height readable after Windows
  downscales the icon to 16 px.

## [1.7.0] - 2026-07-24

### Added
- **CREDIT (THIS MONTH) graph** plots cumulative usage-credit spend over the
  current month, with a dashed red limit line at your monthly cap and a "Now"
  marker. The Y axis ranges from $0 to your limit; month-to-date spend comes
  live from the API and stays accurate even when the meter isn't running. Credit
  history keeps 45 days of samples (one per 30 minutes). Toggled via tray menu
  → *Usage graph* → "Show credit graph" (on by default); shows only on accounts
  with extra usage credits enabled.
- **Credit-burn notification** — a balloon tip fires when usage credits start
  being consumed (spend increases between polls), throttled to at most one per
  2 hours, e.g. "Usage credits are being consumed — $3.13 this month." The first
  poll after startup is a silent baseline, and the monthly reset never triggers a
  false alarm.

### Changed
- **"Extra usage" and "Spend" rows are merged into one.** The Anthropic usage API
  reports the same usage-credit wallet twice (`extra_usage` and `spend`); previous
  versions showed both as two nearly identical percentage rows. They are now
  unified in a single "Extra usage" row that displays the real amounts and dollar
  value, e.g. "Extra usage: 6% — $3.13 / $50.00" (month-to-date spend vs the
  monthly cap, read live from the API).

## [1.6.2] - 2026-07-17

### Fixed
- **The meter finds the Claude Desktop login again after Desktop's switch to MSIX
  packaging.** The 2026-07-17 Desktop auto-update moved its data folder from
  `%APPDATA%\Claude` to `%LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude`
  and deleted the old folder, so the meter could no longer read Desktop's token and
  showed "Usage temporarily unavailable" until the CLI was next used. The meter now
  checks both locations on every poll (freshest `config.json` wins), so it follows
  the folder wherever a future update puts it — even mid-run.

## [1.6.1] - 2026-07-17

### Changed
- **The tray menu is reorganized into feature-grouped submenus.** *Session context*,
  *Usage graph* and *Appearance* each own their toggle and options, with separators
  dividing popup content, window/tray behavior, and app items — 24 flat items down to 16.

### Fixed
- **The session graph no longer fabricates a line across periods when the meter wasn't
  running.** Samples more than 15 minutes apart are drawn as separate segments; each hole
  shows a faint band with a dashed connector and a "no data" label — including holes at
  the window edges (e.g. a 12h view whose left half predates the oldest sample). The
  reset-marker detection also can't pin a reset at the wrong time across a hole anymore,
  and the current-value dot appears from the very first sample after a restart.
- **One unreadable transcript no longer blanks the whole SESSION CONTEXT section.**
  If a session's transcript fails to read mid-poll (rotated/locked), only that session
  is skipped for the cycle; the others still show.
- **Long project names no longer overlap the token readout.** The popup now measures
  each session row's text and widens itself accordingly.

## [1.6.0] - 2026-07-16

### Added
- **SESSION CONTEXT now shows every active session, not just one.** Each session gets its
  own block with the bold session name, percentage, and a `current / capacity` token
  readout (e.g. `169k / 1.0M`) so the window size is explicit. Two new options: *Context:
  max shown* (1/2/3/5) and *Context: sort by* (last active / name A–Z / context high→low).
- When the 5-hour window has reset to 0% and is idle, the Session row now reads
  **"resets 5h after next use"** instead of leaving the reset area blank — the next window
  (and its 5-hour clock) only begins on your next use.

### Fixed
- **SESSION CONTEXT percentage was wrong** on accounts with a 1M context window.
  The meter guessed the window from the token count (`>200k ⇒ 1M, else 200k`), so a
  session at 168k tokens on a 1M window showed **84%** instead of the correct **17%**.
  The window is now inferred from the model (Opus/Sonnet ⇒ 1M, Haiku ⇒ 200k), matching
  what Claude itself reports.

## [1.5.0] - 2026-07-13

### Added
- The meter now also reads the login from the **Claude Desktop app**, not just the
  Claude Code CLI. If you work in Desktop's Code tab and never run the CLI, the CLI
  token would go stale within hours; the meter now falls back to Desktop's own token
  (which Desktop keeps fresh), so your usage stays live without ever opening a terminal.
  Read-only like the CLI source — it decrypts Desktop's local token cache in place and
  never writes or refreshes anything.

### Changed
- The meter no longer refreshes Claude Code's OAuth token itself — every login source
  is now strictly read-only. This removes all traffic to the rate-limited token
  endpoint and guarantees the meter can never disrupt Claude Code's own login session.
  While a login source keeps its token fresh the meter updates as usual; when every
  source has gone stale, the meter shows its last-known usage as stale and updates
  again the next time you use Claude.

### Fixed
- The meter no longer shows a red "Claude login expired" when you are still signed in.
  That alarming state (and the "Fix Claude login" button) is now reserved for a genuine
  sign-out — no login found at all, or the usage endpoint rejecting the token — while
  temporary conditions just keep showing your last-known usage as amber "stale".

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
