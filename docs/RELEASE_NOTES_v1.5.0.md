# Claude Meter v1.5.0

Now works with the Claude Desktop app.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## New

- **Works with the Claude Desktop app** — the meter now reads your login from
  Claude Desktop, not just the Claude Code CLI. If you work in Desktop's Code tab
  and never open a terminal, your CLI token used to go stale within hours and the
  meter would stop updating. It now falls back to Desktop's own login (which stays
  fresh as you use it), so your usage stays live automatically. Read-only, like the
  CLI source — it never writes or refreshes anything.

## Improved

- **The meter can no longer disrupt your Claude login.** Every login source is now
  strictly read-only — the app never refreshes or rotates a token itself. This also
  stops all traffic to Anthropic's rate-limited token endpoint. When every login has
  gone stale, the meter shows your last-known usage as stale and updates again the
  next time you use Claude.

## Fixed

- No more red **"Claude login expired"** when you are still signed in. That alarming
  state (and the "Fix Claude login" button) is now reserved for a genuine sign-out —
  no login found, or the usage endpoint rejecting your token — while a temporary
  hiccup just keeps showing your last-known usage as amber "stale".

Full history in [CHANGELOG.md](../CHANGELOG.md).
