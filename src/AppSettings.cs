using System.Text.Json;

namespace ClaudeMeter;

sealed class AppSettings
{
    public bool AlwaysOnTop { get; set; }
    public string PopupSize { get; set; } = "small"; // small | medium | big
    public int? PinnedX { get; set; }
    public int? PinnedY { get; set; }
    public int OpacityPercent { get; set; } = 100;   // 100 | 85 | 70 | 55 | 40
    public bool ClickThrough { get; set; }           // only effective while pinned
    public int NotifyThreshold { get; set; } = 90; // 0 = off, else 50..95 step 5
    public bool ShowRemainingGraph { get; set; } = true; // session-remaining chart at popup bottom
    public int GraphRangeHours { get; set; } = 24;       // total axis width: 24 or 12
    public int NowPositionPercent { get; set; } = 75;    // where "now" sits on the axis: 50 | 75 | 100
    public string Theme { get; set; } = "dark";          // dark | light
    public bool HotkeyEnabled { get; set; } = true;      // Ctrl+Alt+U toggles the popup
    public bool CheckUpdates { get; set; } = true;       // daily GitHub release check
    public bool ShowLogo { get; set; } = true;       // logo header in the popup

    static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeMeter", "settings.json");

    public float Scale => PopupSize switch
    {
        "medium" => 1.3f,
        "big" => 1.6f,
        _ => 1.0f,
    };

    public static AppSettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // settings persistence is best-effort; never crash the app over it
        }
    }
}
