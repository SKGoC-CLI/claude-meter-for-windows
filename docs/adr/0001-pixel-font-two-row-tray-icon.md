# Two-row tray icon uses a hand-rolled bitmap font, not Segoe UI

The single-number tray icon is drawn with Segoe UI on a 32x32 bitmap that Windows
downscales to 16x16. That works for one number but destroys two: at 100% scaling the
tray gives us 16 real pixels, and every TrueType layout we rendered — two numbers
side by side, and two stacked — was illegible after the downscale, even at 8x
magnification. So the `both` mode (ADR context: `Tray icon mode` in CONTEXT.md) draws
a hand-rolled 5x7 bitmap font (3x5 for three-digit values) directly at the final icon
size, with every glyph pixel as a `k x k` rectangle where `k` is an integer scale
derived from `SystemInformation.SmallIconSize`. Nothing is ever rescaled, so nothing
blurs, at any DPI.

## Considered options

- **Two numbers side by side with a vertical bar between** — the first shape we tried.
  Each number gets ~7 px of width; the right-hand number was mush. Rejected on evidence,
  not taste.
- **One big number plus a small corner badge** — the badge was unreadable at any size
  that left the main number legible.
- **Keep one number and encode the second as bars/dots** — readable, but the whole
  request was to see both *numbers*.
- **Render at 32x32 and let Windows downscale, as the existing code does** — this is
  precisely what fails; it is the reason this ADR exists.

## Consequences

- There are now two text-rendering paths in `IconRenderer`. Single-row mode deliberately
  stays on Segoe UI: switching it to the bitmap font would need a third, larger glyph set
  (~7x12) drawn by hand, which buys sharpness for something already legible.
- The glyph tables are data that can silently rot — a malformed row renders as garbage
  that only a human would notice. `--icon-selftest` (see `src/IconSelfTest.cs`) validates
  every digit's row and column counts, and is the reason that check exists.
- Three-digit values fall back to a smaller 3x5 glyph, so a row reading `100` is visibly
  shorter than one reading `80`. That is a deliberate trade for fitting 100% at all.
