# Claude Meter v1.7.1

The tray icon tells the truth at 100 %, and the credit graph zooms in.

![Claude Usage Meter v1.7.1 — the popup with the credit graph on its new 24-hour range, showing the hourly time axis](https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/screenshot-v1.7.1.png)

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Fixed

- **The tray icon showed "99" when a limit was actually at 100 %.** Three digits
  overflow the 32-pixel icon, so the number was clamped at 99 — meaning a fully
  consumed session read as one percent short while the popup and tooltip
  correctly said 100 %. The clamp is gone. When the text no longer fits it is
  squeezed horizontally rather than set in a smaller font, which keeps the digits
  legible after Windows scales the icon down to 16 pixels.

## Added

- **24-hour and 3-day ranges for the credit graph** — tray menu → *Credit graph*
  → *Range* now offers **24 hours** and **3 days** next to 7, 15 and 30 days.
  Both short ranges switch the time axis to hourly ticks, labelled every 3 hours
  on the 24-hour view and every 12 hours on the 3-day view, with the date shown
  at midnight. The Y axis still runs from $0 to your monthly limit on every
  range, so a short view reads as a nearly flat line by design — what it buys you
  is resolution in time, not in dollars.
- **Month-reset marker.** Credit spend accumulates within a calendar month and
  restarts at $0 on the 1st, which used to appear as an unexplained cliff in the
  graph. A dashed red vertical line labelled "reset" now marks that boundary on
  every range.

Full history in [CHANGELOG.md](../CHANGELOG.md).
