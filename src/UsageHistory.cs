using System.Text.Json;

namespace ClaudeMeter;

/// <summary>Rolling 24-hour usage history per window, persisted to %APPDATA%.</summary>
sealed class UsageHistory
{
    static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeMeter", "history.json");

    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    // "credit_spend" needs month-scale history for the credit chart, so it gets its
    // own retention: 45 days, downsampled instead of the 24h window everything else uses.
    static readonly TimeSpan CreditWindow = TimeSpan.FromDays(45);
    const double CreditBucketSeconds = 30 * 60;
    const string CreditKey = "credit_spend";

    // window key -> list of [unixSeconds, utilization] ("credit_spend" holds dollars, not %)
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

            // dollars go under their own key — old installs already have % samples
            // under "extra_usage", and this is a different unit entirely
            if (w.Key == "extra_usage" && w.UsedDollars is { } dollars)
            {
                if (!_data.TryGetValue(CreditKey, out var creditList))
                    _data[CreditKey] = creditList = new();
                creditList.Add(new[] { now, dollars });
            }
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
        double creditCutoff = DateTimeOffset.UtcNow.Subtract(CreditWindow).ToUnixTimeSeconds();
        foreach (var (key, list) in _data)
        {
            if (key == CreditKey)
            {
                list.RemoveAll(p => p.Length < 2 || p[0] < creditCutoff);
                DownsampleCredit(list);
            }
            else
            {
                list.RemoveAll(p => p.Length < 2 || p[0] < cutoff);
            }
        }
    }

    /// <summary>Keeps at most one (the latest) sample per 30-minute bucket. Values are
    /// cumulative/monotonic within a month, so collapsing a bucket to its last point
    /// loses no shape — it just keeps 45 days of history from growing unbounded.</summary>
    static void DownsampleCredit(List<double[]> list)
    {
        if (list.Count < 2) return;
        list.Sort((a, b) => a[0].CompareTo(b[0]));
        var kept = new List<double[]>();
        foreach (var p in list)
        {
            double bucket = Math.Floor(p[0] / CreditBucketSeconds);
            if (kept.Count > 0 && Math.Floor(kept[^1][0] / CreditBucketSeconds) == bucket)
                kept[^1] = p;
            else
                kept.Add(p);
        }
        list.Clear();
        list.AddRange(kept);
    }

    public IReadOnlyList<double[]> Samples(string key) =>
        _data.TryGetValue(key, out var list) ? list : Array.Empty<double[]>();

    /// <summary>Timestamp of the most recent successful data fetch, if any.</summary>
    public DateTimeOffset? LastSampleTime()
    {
        double max = 0;
        foreach (var list in _data.Values)
            foreach (var p in list)
                if (p[0] > max) max = p[0];
        return max > 0 ? DateTimeOffset.FromUnixTimeSeconds((long)max).ToLocalTime() : null;
    }
}
