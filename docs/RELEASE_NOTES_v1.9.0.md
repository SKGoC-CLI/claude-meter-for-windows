# Claude Meter v1.9.0

Get the popup out of your way, and know how much longer you can keep working.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/popup-and-mini-v1.9.0.png" alt="Claude Meter v1.9.0 — the full popup with a minimize button in the top-right corner and a burn-rate ETA reading full in ~2h 41m under the Session bar, and below it the same meter collapsed to a single Mini mode bar reading 5h 29% and W 34%">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **Minimize button in the popup's top-right corner.** Click "−" to hide the popup
  without reaching for the tray icon or pressing Esc. It hides the window only — your
  *Always on top* setting stays untouched, so a pinned popup comes back pinned in the
  same place. This solves the blind spot where a pinned popup has no visible way to
  dismiss it by mouse.

- **Mini mode collapses the popup to a single bar.** Tray menu → **Mini mode** shrinks
  the whole popup to one line reading `5h 29% · W 34%`, with each number coloured by
  its own severity and sized to fit. It is for people who want the usage numbers on
  screen all day without giving up space. Turning mini mode on pins the popup
  automatically — a bar that vanishes each time you click elsewhere would be useless —
  and turning it off restores your previous pin setting. The weekly number follows the
  same rule as the two-row tray icon: it is the highest of *all* your weekly windows.
  The bar always shows both numbers regardless of your *Show limits* choices, so hiding
  a row in the full popup never blanks it.

- **Burn-rate ETA under each limit.** A small line under each progress bar estimates how
  long until that limit fills at your current pace — for example, "full in ~2h 41m" —
  answering the question the percentage alone cannot: how much longer can I keep
  working? Toggle it with **Show ETA** at the bottom of the *Show limits* menu. The
  pace is measured over a window matched to the limit's own length: one hour for the
  5-hour session, 24 hours for weekly limits, because an hour of heavy use extrapolated
  across a week predicts a wall that a night's sleep can erase. The estimate is
  deliberately quiet — it stays hidden when you are idle, when there is less than
  10 minutes of history to measure, and whenever the limit would reset before it ever
  fills. If you don't see it, the meter is not broken; those rules are how it works.

Full history in [CHANGELOG.md](../CHANGELOG.md).
