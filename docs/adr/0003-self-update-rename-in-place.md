# Self-update replaces the exe in place, trusting GitHub's sha256

The meter updates itself when the user clicks **Update now**: it downloads the release's
`ClaudeMeter-portable.zip` from github.com, checks it against the `sha256` digest the GitHub
API publishes for that asset, checks the exe's file version matches the release tag, then
renames the running `ClaudeMeter.exe` to `ClaudeMeter.old.exe`, moves the new exe into its
place and starts it. The new process deletes `ClaudeMeter.old.exe` as soon as it is up.

Windows lets a running exe be renamed (not overwritten), so no helper script or second
process is needed — which also means no console window can flash over a fullscreen game.

## Considered options

- **Silent auto-update** — rejected: one bad release reaches every user at once, and an app
  that swaps itself unasked is surprising. The user always confirms.
- **Code-signing + signature check** — strongest (also clears SmartScreen), but costs a yearly
  certificate and identity verification. Not now; the sha256 check covers corrupted and
  tampered-in-transit downloads, which are the realistic failures. It does **not** protect
  against a compromised GitHub account — the attacker could upload a zip with a new digest.
  That risk equals today's manual download, so the update path adds none.
- **Keep `.old.exe` as a rollback** — rejected by the owner in favour of a clean folder; if a
  release is bad the user downloads the previous zip from GitHub.
- **Helper .bat / updater exe that waits and copies** — more moving parts and a visible window.

## Consequences

- Release process must keep the asset name `ClaudeMeter-portable.zip` with the exe at its root,
  and the exe's file version must equal the tag. A renamed asset silently disables updating
  (falls back to opening the release page).
- An install in a non-writable folder (Program Files) cannot self-update; it falls back to the page.
- Users on v1.12.0 or older have no updater — they download v1.13.0 by hand one last time.
