namespace ClaudeMeter;

/// <summary>Dark/Light palette shared by the popup and About dialog.</summary>
static class Theme
{
    public static bool Light { get; set; }

    public static Color Background => Light ? ColorTranslator.FromHtml("#fbfbfb") : ColorTranslator.FromHtml("#1e1e1e");
    public static Color Track => Light ? ColorTranslator.FromHtml("#e3e3e3") : ColorTranslator.FromHtml("#3a3a3a");
    public static Color Label => Light ? ColorTranslator.FromHtml("#1f1f1f") : ColorTranslator.FromHtml("#e8e8e8");
    public static Color Muted => Light ? ColorTranslator.FromHtml("#5d5d5d") : ColorTranslator.FromHtml("#8a8a8a");
    public static Color Grid => Light ? Color.FromArgb(24, 0, 0, 0) : Color.FromArgb(38, 255, 255, 255);
    public static Color GridStrong => Light ? Color.FromArgb(60, 0, 0, 0) : Color.FromArgb(80, 255, 255, 255);
    public static Color NowLine => Light ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(120, 255, 255, 255);
    public static Color NowText => Light ? Color.FromArgb(200, 0, 0, 0) : Color.FromArgb(170, 255, 255, 255);

    // Status colours for the popup only. IconRenderer keeps its own brighter set for
    // the tray icon: that one sits on the taskbar, which follows Windows' theme rather
    // than this setting, so darkening it would hide the digits on a dark taskbar.
    // The light values are Windows 11 Fluent semantic colours, all >= 4.5:1 on Background;
    // the dark values are IconRenderer's, so dark mode is byte-for-byte unchanged.
    public static Color Accent => Light ? ColorTranslator.FromHtml("#005FB8") : IconRenderer.Accent;
    public static Color Warning => Light ? ColorTranslator.FromHtml("#9D5D00") : IconRenderer.Warning;
    public static Color Danger => Light ? ColorTranslator.FromHtml("#C42B1C") : IconRenderer.Danger;
    public static Color Success => Light ? ColorTranslator.FromHtml("#0F7B0F") : ColorTranslator.FromHtml("#6bcb77");

    /// <summary>Severity colour for a utilization percentage — same thresholds as
    /// IconRenderer.ColorFor, but theme-aware. Popup code wants this one.</summary>
    public static Color ColorFor(double utilization) =>
        utilization >= 90 ? Danger : utilization >= 70 ? Warning : Accent;

    /// <summary>Popup window border as a COLORREF for DWMWA_BORDER_COLOR. Light needs a
    /// real edge — #fbfbfb over a white window behind it has none. Dark keeps the system
    /// default, which already reads against anything.</summary>
    public static int BorderColorRef =>
        Light ? 0xE0E0E0 : unchecked((int)0xFFFFFFFF); // 0x00BBGGRR / DWMWA_COLOR_DEFAULT
}
