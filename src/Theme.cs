namespace ClaudeMeter;

/// <summary>Dark/Light palette shared by the popup and About dialog.</summary>
static class Theme
{
    public static bool Light { get; set; }

    public static Color Background => Light ? ColorTranslator.FromHtml("#f4f4f4") : ColorTranslator.FromHtml("#1e1e1e");
    public static Color Track => Light ? ColorTranslator.FromHtml("#d8d8d8") : ColorTranslator.FromHtml("#3a3a3a");
    public static Color Label => Light ? ColorTranslator.FromHtml("#1f1f1f") : ColorTranslator.FromHtml("#e8e8e8");
    public static Color Muted => Light ? ColorTranslator.FromHtml("#6d6d6d") : ColorTranslator.FromHtml("#8a8a8a");
    public static Color Grid => Light ? Color.FromArgb(30, 0, 0, 0) : Color.FromArgb(38, 255, 255, 255);
    public static Color GridStrong => Light ? Color.FromArgb(70, 0, 0, 0) : Color.FromArgb(80, 255, 255, 255);
    public static Color NowLine => Light ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(120, 255, 255, 255);
    public static Color NowText => Light ? Color.FromArgb(200, 0, 0, 0) : Color.FromArgb(170, 255, 255, 255);
}
