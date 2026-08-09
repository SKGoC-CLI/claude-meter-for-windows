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

    /// <summary>Keeps at most one (the latest) sample per 30-minute bucket, but only for
    /// samples older than 24 h — fresh samples stay at full poll resolution so the chart
    /// has points to draw right away instead of waiting out a whole bucket. Values are
    /// cumulative/monotonic within a month, so collapsing old buckets loses no shape.</summary>
    static void DownsampleCredit(List<double[]> list)
    {
        if (list.Count < 2) return;
        list.Sort((a, b) => a[0].CompareTo(b[0]));
        double freshCutoff = DateTimeOffset.UtcNow.Subtract(Window).ToUnixTimeSeconds();
        var kept = new List<double[]>();
        foreach (var p in list)
        {
            double bucket = Math.Floor(p[0] / CreditBucketSeconds);
            if (p[0] < freshCutoff && kept.Count > 0 &&
                kept[^1][0] < freshCutoff && Math.Floor(kept[^1][0] / CreditBucketSeconds) == bucket)
                kept[^1] = p;
            else
                kept.Add(p);
        }
        list.Clear();
        list.AddRange(kept);
    }

    public IReadOnlyList<double[]> Samples(string key) =>
        _data.TryGetValue(key, out var list) ? list : Array.Empty<double[]>();

    /// <summary>Utilisation (or dollars) gained per second over the last <paramref name="lookback"/>,
    /// or null when there are too few samples / too short a span to mean anything.
    /// Non-credit history is pruned to 24 h (see Window above), so a 24 h lookback is
    /// the practical maximum for any key other than "credit_spend".</summary>
    public double? RatePerSecond(string key, TimeSpan lookback)
    {
        double cutoff = DateTimeOffset.UtcNow.Subtract(lookback).ToUnixTimeSeconds();
        var kept = Samples(key).Where(p => p[0] >= cutoff).OrderBy(p => p[0]).ToList();
        if (kept.Count < 2) return null;

        // These series only ever climb within a window, so a fall means the window reset.
        // Measuring across that cliff averages the old window into the new one, so start
        // after the last one. (> 1 point/dollar, to ignore rounding wobble.)
        for (int i = kept.Count - 1; i > 0; i--)
            if (kept[i][1] < kept[i - 1][1] - 1)
            {
                kept = kept.GetRange(i, kept.Count - i);
                break;
            }
        if (kept.Count < 2) return null;

        var first = kept[0];
        var last = kept[^1];
        if (last[0] - first[0] < 600) return null; // too short a span to trust the slope

        double rate = (last[1] - first[1]) / (last[0] - first[0]);
        return rate > 0 ? rate : null; // idle or just reset — an ETA there is noise
    }

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
