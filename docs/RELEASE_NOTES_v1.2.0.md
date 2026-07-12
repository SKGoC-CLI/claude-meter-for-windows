# Claude Meter v1.2.0

The "your real limits, honestly" update.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Fixed

- **Model-scoped weekly limits (e.g. "Fable Weekly") were missing.** Anthropic
  moved them into a new `limits` array in the usage API; the parser now reads
  that shape first, so every limit on your account shows up again — including
  the one most likely to cut you off.

## New

- **Tray icon shows the limit that actually matters.** New default
  "Active limit (auto)" trusts the server's own flag for whichever limit is
  currently binding; manual choices (Session / Weekly / Highest) remain, and
  anything at 90 %+ always takes over the icon
- Live **hh:mm:ss clock** at the graph's "Now" marker; graph retitled
  "Session Graph"
- **Extra-usage credits and spend** appear as rows when enabled on the account

## Improved

- Token refresh now backs off politely when rate-limited (honors `Retry-After`,
  10 min doubling to a 2 h cap, instant retry after a fresh login) instead of
  retrying every 3 minutes

Full history in [CHANGELOG.md](../CHANGELOG.md).
