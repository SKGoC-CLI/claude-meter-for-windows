namespace ClaudeMeter;

/// <summary>
/// Runnable check for RenderTwoRow, invoked via `--icon-selftest`. Not a test
/// project — just enough to catch a malformed glyph or a crash before release.
/// </summary>
static class IconSelfTest
{
    public static void Run()
    {
        int twoRowSize = 16 * Math.Max(1, SystemInformation.SmallIconSize.Width / 16);

        foreach (var (session, weekly) in new (double?, double?)[]
                 {
                     (0, 80), (100, 100), (null, 80), (7, null), (null, null),
                 })
        {
            // both-null falls back to Render(null), which draws on a fixed 32x32
            // canvas — every other combination draws the two-row icon at twoRowSize.
            int expectedSize = session is null && weekly is null ? 32 : twoRowSize;

            var icon = IconRenderer.RenderTwoRow(session, weekly);
            if (icon is null) throw new Exception($"RenderTwoRow({session}, {weekly}) returned null");
            if (icon.Width != expectedSize || icon.Height != expectedSize)
                throw new Exception($"RenderTwoRow({session}, {weekly}) size {icon.Width}x{icon.Height}, expected {expectedSize}x{expectedSize}");
            icon.Dispose();
        }

        CheckFont(IconRenderer.Font5x7, expectedRows: 7, expectedCols: 5, name: "Font5x7");
        CheckFont(IconRenderer.Font3x5, expectedRows: 5, expectedCols: 3, name: "Font3x5");

        Console.WriteLine("icon selftest OK");
    }

    static void CheckFont(string[][] font, int expectedRows, int expectedCols, string name)
    {
        if (font.Length != 10) throw new Exception($"{name} has {font.Length} digits, expected 10");
        for (int digit = 0; digit < 10; digit++)
        {
            var glyph = font[digit];
            if (glyph.Length != expectedRows)
                throw new Exception($"{name}[{digit}] has {glyph.Length} rows, expected {expectedRows}");
            foreach (var row in glyph)
                if (row.Length != expectedCols)
                    throw new Exception($"{name}[{digit}] row \"{row}\" has length {row.Length}, expected {expectedCols}");
        }
    }
}
