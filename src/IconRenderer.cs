using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ClaudeMeter;

static class IconRenderer
{
    public static readonly Color Accent = ColorTranslator.FromHtml("#4da3ff");
    public static readonly Color Warning = ColorTranslator.FromHtml("#ffa940");
    public static readonly Color Danger = ColorTranslator.FromHtml("#ff5c5c");

    public static Color ColorFor(double utilization) =>
        utilization >= 90 ? Danger : utilization >= 70 ? Warning : Accent;

    /// <summary>
    /// Draws the tray icon: the highest utilization as a number with a thin
    /// fill bar underneath, color-coded by severity. Gray "--" when no data.
    /// Caller owns the returned icon and must destroy its handle via Dispose.
    /// </summary>
    public static Icon Render(double? maxUtilization)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            var color = maxUtilization is { } u ? ColorFor(u) : Color.Gray;
            string text = maxUtilization is { } v ? Math.Clamp((int)Math.Round(v), 0, 100).ToString() : "--";

            using var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            var textSize = g.MeasureString(text, font);
            using var textBrush = new SolidBrush(Color.White);
            // "100" overflows at full width, so squeeze it horizontally instead of
            // shrinking the font — glyph height is what survives the 16 px downscale.
            float scaleX = textSize.Width > size - 1 ? (size - 1) / textSize.Width : 1f;
            var state = g.Save();
            g.ScaleTransform(scaleX, 1f);
            g.DrawString(text, font, textBrush, (size / scaleX - textSize.Width) / 2f, 2f);
            g.Restore(state);

            // fill bar along the bottom
            using var trackBrush = new SolidBrush(Color.FromArgb(90, 255, 255, 255));
            g.FillRectangle(trackBrush, 2, size - 8, size - 4, 5);
            if (maxUtilization is { } pct)
            {
                using var fillBrush = new SolidBrush(color);
                int width = (int)Math.Round((size - 4) * Math.Clamp(pct, 0, 100) / 100.0);
                if (width > 0) g.FillRectangle(fillBrush, 2, size - 8, width, 5);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // Clone so the icon owns its own data, then release the GDI handle.
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    // -- two-row bitmap-font icon -------------------------------------------
    //
    // Render() relies on Segoe UI downscaled from 32px to 16px by Windows — fine
    // for one number, but two stacked numbers turn to mush under that downscale
    // (measured, side-by-side and stacked TrueType both fail). RenderTwoRow draws
    // a hand-rolled pixel font directly at the final icon size instead, so there
    // is no rescale step to blur it.

    // 5x7 glyphs (default), rows top->bottom, '1' = ink.
    internal static readonly string[][] Font5x7 =
    {
        new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" }, // 0
        new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" }, // 1
        new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" }, // 2
        new[] { "11111", "00010", "00100", "00010", "00001", "10001", "01110" }, // 3
        new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" }, // 4
        new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" }, // 5
        new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" }, // 6
        new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" }, // 7
        new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" }, // 8
        new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" }, // 9
    };

    // 3x5 fallback, used only for a 3-digit value (100) — 5x7 doesn't fit three
    // digits in a 16px-wide row.
    internal static readonly string[][] Font3x5 =
    {
        new[] { "111", "101", "101", "101", "111" }, // 0
        new[] { "010", "110", "010", "010", "111" }, // 1
        new[] { "111", "001", "111", "100", "111" }, // 2
        new[] { "111", "001", "111", "001", "111" }, // 3
        new[] { "101", "101", "111", "001", "001" }, // 4
        new[] { "111", "100", "111", "001", "111" }, // 5
        new[] { "111", "100", "111", "101", "111" }, // 6
        new[] { "111", "001", "001", "001", "001" }, // 7
        new[] { "111", "101", "111", "101", "111" }, // 8
        new[] { "111", "101", "111", "001", "111" }, // 9
    };

    enum RowSlot { Top, Bottom, Center }

    /// <summary>
    /// Draws the tray icon as two stacked rows — Session (5h) on top, Weekly on
    /// the bottom — each colour-coded by its own severity, using a bitmap font
    /// at the exact final icon size (no downscale to blur them). Falls back to
    /// the single-number "--" icon when both values are missing; a single
    /// present value is drawn vertically centred rather than leaving a blank row.
    /// Caller owns the returned icon and must destroy its handle via Dispose.
    /// </summary>
    public static Icon RenderTwoRow(double? session, double? weekly)
    {
        if (session is null && weekly is null) return Render(null);

        // Integer pixel scale only — a fractional one would reintroduce the very
        // resampling this font exists to avoid. The canvas is then 16*k rather than
        // the tray's own slot size: at 125/150 % the slot (20/24 px) is not a
        // multiple of 16, and centring a 16 px grid inside it would leave the
        // two-row icon visibly smaller than every other mode. Handing Windows a
        // full-bleed 16 px icon to scale up costs a little softness there and
        // stays pixel-exact at 100 % and 200 %.
        int k = Math.Max(1, SystemInformation.SmallIconSize.Width / 16);
        int size = 16 * k;

        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Color.Transparent);

            if (session is { } s && weekly is { } w)
            {
                DrawRow(g, s, RowSlot.Top, k);
                DrawRow(g, w, RowSlot.Bottom, k);
            }
            else if (session is { } sOnly)
            {
                DrawRow(g, sOnly, RowSlot.Center, k);
            }
            else if (weekly is { } wOnly)
            {
                DrawRow(g, wOnly, RowSlot.Center, k);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    /// <summary>Draws one row of digits at a logical 16x16 position, scaled by k.</summary>
    static void DrawRow(Graphics g, double value, RowSlot slot, int k)
    {
        string text = Math.Clamp((int)Math.Round(value), 0, 100).ToString();
        bool small = text.Length == 3;
        var font = small ? Font3x5 : Font5x7;
        int glyphW = small ? 3 : 5;
        int glyphH = small ? 5 : 7;
        int width = text.Length * (glyphW + 1) - 1;
        int x = (16 - width) / 2;
        int y = slot switch
        {
            RowSlot.Top => small ? 1 : 0,
            RowSlot.Bottom => small ? 10 : 9,
            _ => small ? 5 : 4, // Center
        };

        using var brush = new SolidBrush(ColorFor(value));
        for (int i = 0; i < text.Length; i++)
        {
            var glyph = font[text[i] - '0'];
            for (int row = 0; row < glyphH; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < glyphW; col++)
                {
                    if (line[col] != '1') continue;
                    int px = (x + i * (glyphW + 1) + col) * k;
                    int py = (y + row) * k;
                    g.FillRectangle(brush, px, py, k, k);
                }
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);
}
