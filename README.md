# Claude Usage Meter for Windows

<img src="assets/logo.png" width="96" align="right" alt="logo">

[![Build](https://github.com/SKGoC-CLI/claude-meter-for-windows/actions/workflows/build.yml/badge.svg)](https://github.com/SKGoC-CLI/claude-meter-for-windows/actions)
![Windows 10/11](https://img.shields.io/badge/Windows-10%2F11-0078d4)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Latest release](https://img.shields.io/github/v/release/SKGoC-CLI/claude-meter-for-windows)](../../releases/latest)

<p align="center">
  <img src="docs/demo-v1.12.0.gif" width="640" alt="Claude Meter v1.12.0 demo — the popup open above the Windows tray; the credit line reads Cloud credit $94.79 left · Usage credits $5.00 left, and under CURRENT SESSIONS (3) the first session shows a row of step segments with 7/7 release v1.12.0">
</p>

A tiny Windows system-tray app that shows your **Claude usage limits** in real time —
the same *Session (5h) / Weekly / per-model weekly* percentages you see in
Claude Code's `/usage`, always one click away.

*New in v1.12.0 — your purchased usage credits sit next to the cloud credit on one line, and each session's progress is a short row of segments with the current step's headline:*

<p align="center">
  <img src="docs/popup-v1.12.0.png" alt="Claude Meter v1.12.0 — dark-theme popup; the credit line under Monthly spend, and the first current session showing three of six step segments filled with 3/6 Draw the goal progress row">
</p>

*The tray icon can show both limits at once, Session (5h) stacked over Weekly, so a draining week can't hide behind a fresh session:*

<p align="center">
  <img src="docs/tray-two-row-v1.8.0.png" alt="Tray icon in Session + Weekly mode — Session (5h) on the top row, the highest weekly below, each row coloured by its own severity">
</p>

*Every limit, every session's context window, and both graphs — one click from the tray:*

<p align="center">
  <img src="docs/screenshot-v1.7.1.png" width="400" alt="Claude Usage Meter popup — limits, session context, and the session and credit graphs">
</p>

*Track your usage credits in real dollars against the monthly cap — a live spending graph with the limit line marked, and a matching dollar row up top:*

![demo — usage-credit spending graph](docs/demo-credit-graph.gif)

*Every active Claude Code session's context window at a glance, with live token counts:*

![demo — multi-session context](docs/demo-multi-session.gif)

*The usage graph is honest about downtime — "no data" bands instead of fabricated lines:*

![demo — usage graph marking downtime as no data](docs/demo-no-data.gif)

## Features

- Tray icon renders your highest usage percentage live, shifting color as you approach the limit; the optional Session + Weekly mode stacks both limits so you catch a draining week at a glance
- Popup shows a progress bar, reset countdown and exact reset time for every limit window
- Monthly spend displayed as real dollar amounts (e.g. "Monthly spend: 6% — $3.13 / $50.00"); purchased usage credits and the cloud credit share one line ("Cloud credit: $94.79 left · Usage credits: $5.00 left"), with the same data from `extra_usage` and `spend` rows merged into one
- Usage graph plots session remaining over a 12 or 24 hour axis, with hourly ticks, reset markers for both past and upcoming resets, and an adjustable "now" position
- Credit spending graph shows cumulative month-to-date usage-credit spend up to your monthly limit, with a dashed limit line and live "now" marker; stays accurate even if the meter isn't running (45-day history sampled every 30 minutes)
- Burn-rate estimate under each limit — "full in ~2h 41m" at your current pace, measured over a window matched to the limit's own length, and kept quiet whenever the limit would reset before it ever fills
- Session state dots in CURRENT SESSION(S): green while Working, amber when Waiting for you or Asking a question, grey when Idle
- Step progress per session — a row of segments `▬▬▬▭▭▭ 3/6  <step headline>` (over 10 steps: `Step 3/14 · <step>`), read from the session's task list (Claude desktop app plan, or TaskCreate/TaskUpdate in the CLI), falling back to an active `NEXT-SESSION.md` sprint table; toggle under *Current session → Show step progress*
- Session alerts: the tray digits dim while a session is Working and the tray turns orange while one is Asking; speed and each effect can be set under *Current session → Session alerts*
- Effort level shown next to each session's model, e.g. "Opus · High"
- Cloud credit row shows the remaining cloud-session credit in dollars and its expiry date; it stays out of the tray icon, alerts and ETA
- Mini mode collapses the whole popup to a single bar (`5h 29% · W 34%`) for people who want the numbers on screen all day
- Pin the popup anywhere on screen; optionally let mouse clicks pass through it while pinned; a minimize button hides it again without unpinning
- Dark and light themes, three popup sizes, five opacity levels with full opacity restored on hover
- Usage alert notifications at any threshold from 50 to 95 percent, plus a credit-burn notification when usage credits start being consumed (at most once per 2 hours)
- One-click login recovery when the Claude session expires, plus local diagnostic logs for troubleshooting
- Global hotkey to toggle the popup — Ctrl+Alt+U by default, seven combos to pick from — plus autostart with Windows, automatic update check, single instance
- Every preference persists across restarts

## Download

Grab `ClaudeMeter-portable.zip` from the **[latest Release](../../releases/latest)** —
a single self-contained exe, no .NET installation required.

> Windows SmartScreen may warn because the exe is not code-signed:
> click **More info → Run anyway**.

## Requirements

- Windows 10/11 (64-bit)
- A Claude login already on this machine — **either one** works:
  - the **Claude Desktop app**, signed in — nothing else to set up, or
  - the **[Claude Code](https://claude.com/claude-code) CLI**, logged in once:

    ```
    claude
    /login
    ```

  The app reads the OAuth token your Claude login already stores locally —
  it never sees your password and **never modifies** those files.

## Usage

| Action | Result |
|---|---|
| Left-click tray icon | Show/hide the usage popup |
| Right-click tray icon | Everything else, in four groups — **Popup shows** (Limits / Current session / Graphs), **Window** (always on top, mini mode, pin, theme, size, opacity, hotkey), **Tray & alerts**, **App** (autostart, updates, log, About) |
| Drag (while pinned) | Move the popup anywhere; position is remembered |
| Click "−" (popup, top-right) | Hide the popup; pinning is left as it was |
| Esc | Hide the popup |

Data refreshes every **3 minutes** — Anthropic's usage endpoint rate-limits
anything faster.

## Build from source

Requires the .NET 8 SDK.

```powershell
dotnet run                                                  # develop
dotnet publish -c Release -r win-x64 --self-contained false `
  /p:PublishSingleFile=true -o publish                      # ~200 KB (needs .NET 8 runtime)
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=none `
  -o portable                                               # ~68 MB portable exe
```

## How it works

The app calls Anthropic's OAuth usage endpoint
(`https://api.anthropic.com/api/oauth/usage`) with the token from your local
Claude login — the same source that powers `/usage`. It reads whichever login is
fresh: the Claude Code CLI's credentials file, or the Claude Desktop app's own
token cache (so it stays live even if you only ever use Desktop's Code tab and
never open a terminal). Usage windows are parsed dynamically, so new limit types
added by Anthropic appear automatically.

Every login source is treated as strictly read-only — the app never refreshes or
rotates a token, so it can't disrupt Claude Code's or Desktop's own session. When
every source has gone stale, the meter shows your last-known usage as stale and
updates again automatically the next time you use Claude.

**Privacy:** no telemetry, no third-party servers. The only network calls are to
Anthropic's own API. Settings and history live in `%APPDATA%\ClaudeMeter\`.

**Troubleshooting:** the app writes small daily logs (activity + errors, never
tokens) to `%APPDATA%\ClaudeMeter\logs` — tray menu → **Open log folder**.
Logs older than 7 days are deleted automatically. Attach the latest log when
reporting an issue.

## Disclaimer

This is an unofficial hobby project, not affiliated with Anthropic.
The usage endpoint is undocumented and may change or stop working at any time.

## License

[MIT](LICENSE)
