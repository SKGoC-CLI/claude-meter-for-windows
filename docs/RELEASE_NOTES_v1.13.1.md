# Claude Meter v1.13.1

Hotfix: finds your Claude Desktop login again after Desktop's 20 October 2026 update.

## Update

On v1.13.0, use **App → Check for updates now → Update now**. The app installs the update and restarts in a few seconds.

On an older version, download **`ClaudeMeter-portable.zip`** (attached), unzip it over your old folder, and run `ClaudeMeter.exe`.
No .NET installation is needed. If SmartScreen warns on first run, choose **More info → Run anyway**.

## Fixed

- **The meter no longer stays on "stale" after the latest Claude Desktop update.**
  - On 20 October 2026, Desktop moved its login data from the MSIX package folder to `%LOCALAPPDATA%\Claude-Data\`.
  - The meter was still looking in the old location. If you only use the Desktop app, it lost your login and showed **"Usage temporarily unavailable"**, with the last saved numbers marked stale.
  - The meter now also checks the new location, and as before it uses whichever location has the most recently saved login.
  - If your popup has said *stale* since last night, this release fixes it.

Your settings, saved usage history and login tokens are untouched.

Full history in [CHANGELOG.md](../CHANGELOG.md).
