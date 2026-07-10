using System.Text.Json;

namespace ClaudeMeter;

/// <summary>Rolling 24-hour usage history per window, persisted to %APPDATA%.</summary>
sealed class UsageHistory
{
    static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeMeter", "history.json");

    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    // window key -> list of [unixSeconds, utilization]
    Dictionary<string, List<double[]>> _data = new();

    public UsageHistory()
    {
        try
        {
            _data = JsonSerializer.Deserialize<Dictionary<string, List<double[]>>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch
        {
            _data = new();
        }
        Prune();
    }

    public void Add(UsageSnapshot snapshot)
    {
        double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var w in snapshot.Windows)
        {
            if (!_data.TryGetValue(w.Key, out var list))
                _data[w.Key] = list = new();
            list.Add(new[] { now, w.Utilization });
        }
        Prune();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data));
        }
        catch
        {
            // history is best-effort; never crash the app over it
        }
    }

    void Prune()
    {
        double cutoff = DateTimeOffset.UtcNow.Subtract(Window).ToUnixTimeSeconds();
        foreach (var list in _data.Values)
            list.RemoveAll(p => p.Length < 2 || p[0] < cutoff);
    }

    public IReadOnlyList<double[]> Samples(string key) =>
        _data.TryGetValue(key, out var list) ? list : Array.Empty<double[]>();
}
