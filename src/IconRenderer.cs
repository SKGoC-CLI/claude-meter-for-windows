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
            string text = maxUtilization is { } v ? Math.Min(99, (int)Math.Round(v)).ToString() : "--";

            using var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            var textSize = g.MeasureString(text, font);
            using var textBrush = new SolidBrush(Color.White);
            g.DrawString(text, font, textBrush, (size - textSize.Width) / 2f, 2f);

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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);
}
