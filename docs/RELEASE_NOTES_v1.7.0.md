# Claude Meter v1.7.0

Credit tracking with a live spending graph and merged dollar-amount row.

![Claude Usage Meter v1.7.0 — merged Extra usage dollar row and the new CREDIT graph](https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/screenshot-v1.7.0.png)

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Added

- **CREDIT (THIS MONTH) graph** — a second chart below the 24h session graph plots your
  cumulative usage-credit spend across the current month. The Y axis ranges from $0 to
  your monthly limit, with a dashed red **limit line** at the cap (e.g. "$50.00 limit")
  and a "Now" marker. Month-to-date spend comes live from the API and stays accurate even
  when the meter isn't running. Credit history keeps 45 days of samples (one per 30
  minutes). Toggled via tray menu → *Usage graph* → "Show credit graph" (on by default);
  shows only on accounts with extra usage credits enabled.
- **Credit-burn notification** — a balloon tip fires when usage credits start being
  consumed (spend increases between polls), throttled to at most one per 2 hours,
  e.g. "Usage credits are being consumed — $3.13 this month." The first poll after
  startup is a silent baseline, and the monthly reset never triggers a false alarm.

## Changed

- **"Extra usage" and "Spend" rows are merged into one.** The Anthropic usage API
  reports the same usage-credit wallet twice (`extra_usage` and `spend`); previous
  versions showed both as two nearly identical percentage rows. They are now unified
  in a single "Extra usage" row that displays the real amounts and dollar value,
  e.g. "Extra usage: 6% — $3.13 / $50.00" (month-to-date spend vs the monthly cap,
  read live from the API — never hardcoded).

Full history in [CHANGELOG.md](../CHANGELOG.md).
