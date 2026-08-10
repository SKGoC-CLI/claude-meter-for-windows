# Claude Meter v1.9.1

The light theme is now a real light theme, with readable status colours meeting the 4.5:1 contrast requirement instead of washing out on the pale popup background.

<p align="center">
  <img src="https://raw.githubusercontent.com/SKGoC-CLI/claude-meter-for-windows/main/docs/light-dark-v1.9.1.png" alt="Claude Meter v1.9.1 — the popup rendered in light theme on the left and dark theme on the right, both on a neutral grey backdrop. Light theme shows darker text, a near-white surface, and readable status colours; dark theme is unchanged from v1.9.0.">
</p>

## Download

**`ClaudeMeter-portable.zip`** (attached) — unzip and run `ClaudeMeter.exe`.
No .NET installation needed. SmartScreen on first run: **More info → Run anyway**.

## Changed

- **The light theme was rebuilt around a light background instead of an inverted dark one.** Its status colours had been carried over from the dark theme, where they sit on `#1e1e1e`; on the pale popup background they washed out badly — the percentage number, the single most important thing in the window, ran at 2.4:1 contrast against a 4.5:1 requirement, and the amber 70–89% state at 1.7:1. Light mode now uses the Windows 11 Fluent semantic colours (`#005FB8` / `#9D5D00` / `#C42B1C`, all above 4.5:1), a near-white `#fbfbfb` surface in place of the flat `#f4f4f4` grey that read as a disabled panel, a progress-bar track light enough to be a groove rather than a smear, and darker secondary text for the many 7–8 pt labels. The reset markers on the session graph got a light-mode green too. **Dark mode is unchanged down to the byte** — the new theme-aware colours return the previous dark values verbatim.

- **The popup has a visible edge in light mode.** A `#fbfbfb` window on top of a white one had no discernible boundary. The popup now tints its DWM window border, which keeps the rounded corners Windows already draws; dark mode keeps the system default border.

- **The tray icon keeps its own colours.** The popup's status colours and the tray icon's are now separate. The icon sits on the taskbar, which follows the *Windows* theme rather than this app's setting, so darkening it for a light popup would have hidden the digits on a dark taskbar. The tray icon is untouched by this release.

- **The logo dropped its dark tile.** The header and About-dialog logo was a rounded black square, which turned into a solid dark block on a light background. It is now the ring and chick on transparency, so one file works on both themes. The app icon on the desktop and taskbar still has its tile.

## Upgrading

Unzip over your old installation folder. Settings, saved usage history, and login tokens are untouched. Dark-mode users will see no visual difference at all.

Full history in [CHANGELOG.md](../CHANGELOG.md).
