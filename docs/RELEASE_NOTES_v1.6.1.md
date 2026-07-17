# Claude Meter v1.6.1

A cleaner tray menu, and a session graph that never lies about downtime.

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Improved

- **The tray menu is reorganized into feature-grouped submenus.** *Session context*,
  *Usage graph* and *Appearance* each own their toggle and options, with separators
  dividing popup content, window & tray behavior, and app items — 24 flat items down
  to a tidy 16.

## Fixed

- **The session graph no longer fabricates a line across downtime.** If the meter
  wasn't running (PC off, app closed), that period now renders as a faint band with a
  dashed connector and a **"no data"** label instead of a misleading straight line —
  including at the graph edges, e.g. a 12h view whose left half predates the oldest
  sample. Reset markers can no longer be pinned at the wrong time across such a hole,
  and the current-value dot appears from the very first sample after a restart.
- **One unreadable session transcript no longer blanks the whole SESSION CONTEXT
  section.** If a transcript fails to read mid-poll (being rotated or locked), only
  that session is skipped for the cycle — the rest still show.
- **Long project names no longer overlap the token readout.** The popup measures each
  session row and widens itself to fit.

Full history in [CHANGELOG.md](../CHANGELOG.md).
