# Claude Meter v1.14.0

See at a glance which sessions still have work running in the background.

## Update

On v1.13.0 or later, use **App → Check for updates now → Update now**. The app installs the update and restarts in a few seconds.

On an older version, download **`ClaudeMeter-portable.zip`** (attached), unzip it over your old folder, and run `ClaudeMeter.exe`.
No .NET installation is needed. If SmartScreen warns on first run, choose **More info → Run anyway**.

## Added

- **A teal blinking dot means "turn finished, background task still running".** Covers a Monitor watching a log, a command run in the background, and an agent launched in the background. It blinks more slowly than the green *Working* dot so the two are easy to tell apart. Green (working) and amber (asking you) still win when they apply.
- **Line 2 names the task**: `⟳ queue.log: errors…`, or `⟳ 2 bg: <first task>` when several are running.
- **The dot clears by itself** when the task completes, fails, is killed or stopped, or passes its own timeout. Tasks without a timeout count for at most 3 hours.
- **Long quiet jobs stay visible.** A session with a running background task is no longer dropped after 30 minutes of quiet. A session that stopped mid-turn is still dropped as before.

The tray icon is unchanged. Your settings, saved usage history and login tokens are untouched.

Full history in [CHANGELOG.md](../CHANGELOG.md).
