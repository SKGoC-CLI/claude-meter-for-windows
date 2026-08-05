# Claude Meter v1.8.0

See both limits at once — Session and Weekly stacked in the tray icon, each colored by its own severity.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/tray-two-row-v1.8.0.png" alt="Tray icon in Session + Weekly mode — Session (5h) on the top row, the highest weekly below, each row coloured by its own severity">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **Two-row tray icon mode.** Tray menu → *Tray icon shows* → new option **Session + Weekly**.
  The top row shows your Session (5h) window; the bottom row shows the highest of *all* your
  weekly windows — the plain all-models weekly, or a model-scoped one such as Opus Weekly when
  that is the one closer to binding. Each row is coloured independently by
  its own severity: blue below 70%, orange 70–89%, red at 90%+. This solves a real blind spot —
  after a weekly reset, a few heavy days can drain the week to 80% while the 5-hour session
  reads 0%. The existing *Active limit (auto)* mode only switches to a second window once it
  reaches 90%, so a week can quietly max out while the icon shows a low session percentage.
  The new mode is opt-in; the default stays *Active limit (auto)*, so your icon doesn't
  change on upgrade. No data still shows grey `--`; an account with only one of the two
  windows shows that single number centred. Usage credits (the wallet) never appear on the
  icon — they are money, not a limit that blocks work.

Full history in [CHANGELOG.md](../CHANGELOG.md).
