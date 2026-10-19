# Claude Meter v1.13.0

Claude Meter now updates itself. This is the last version you have to download by hand. After it, **App → Check for updates now** (or the daily check) offers **Update now**, and the app swaps itself and restarts within a few seconds.

## Download

**`ClaudeMeter-portable.zip`** (attached): unzip it and run `ClaudeMeter.exe`.
No .NET installation is needed. If SmartScreen warns on first run, choose **More info → Run anyway**.

## Added

- **One-click update.** When a new version exists, the tray menu item, the balloon and *Check for updates now* all open one prompt: **Update now / What's new / Later**.
  - Update now downloads the release zip from GitHub and verifies its SHA-256 against the digest GitHub publishes.
  - It then checks that the exe inside is the announced version, replaces the exe in place, and restarts.
  - No script or console window appears.
  - If any check fails, your current exe stays untouched and the prompt offers the download page.
  - If the exe sits in a folder the app cannot write to, such as Program Files, it opens the release page instead.
- **Limit resets line.** When your account holds free limit resets, a small line under the credit line reads `Limit resets: 2 of 3 left`, with `expires in 12d (28 Oct)` on the right. It is hidden when none are left, and you can turn it off under *Show limits*.

## Fixed

- **Long session names** no longer overlap the token count or widen the popup. Only the project name is shortened with `…`.

## Upgrading

Unzip over your old installation folder. Your settings, saved usage history and login tokens are untouched.

Full history in [CHANGELOG.md](../CHANGELOG.md).
