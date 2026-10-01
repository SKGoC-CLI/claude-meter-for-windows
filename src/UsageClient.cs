using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace ClaudeMeter;

sealed record UsageWindow(
    string Key,
    string Label,
    double Utilization,
    DateTimeOffset? ResetsAt,
    bool IsActive = false,      // server marks the limit currently binding
    string Severity = "normal",
    double? UsedDollars = null,  // wallet row only: month-to-date spend / cap, in dollars
    double? LimitDollars = null);

sealed record UsageSnapshot(IReadOnlyList<UsageWindow> Windows, DateTimeOffset FetchedAt);

sealed class UsageException : Exception
{
    public TimeSpan? RetryAfter { get; }

    /// <summary>True only when the user must sign in again; false for transient
    /// failures (rate limits, network) where the saved login still recovers on its own.</summary>
    public bool NeedsRelogin { get; }

    public UsageException(string message, TimeSpan? retryAfter = null, bool needsRelogin = false)
        : base(message)
    {
        RetryAfter = retryAfter;
        NeedsRelogin = needsRelogin;
    }
}

/// <summary>
/// Fetches usage windows from Anthropic's OAuth usage endpoint — the same data
/// behind Claude Code's /usage. The endpoint is undocumented and aggressively
/// rate-limited without the claude-code User-Agent; poll no faster than 180 s.
/// </summary>
sealed class UsageClient
{
    const string UsageUrl = "https://api.anthropic.com/api/oauth/usage";

    readonly HttpClient _http;
    readonly CredentialStore _credentials;

    public UsageClient(HttpClient http, CredentialStore credentials)
    {
        _http = http;
        _credentials = credentials;
    }

    public async Task<UsageSnapshot> FetchAsync()
    {
        if (!_credentials.HasAnyLogin)
            throw new UsageException("Claude Code is not logged in.", needsRelogin: true);

        var (token, needsRelogin) = _credentials.GetAccessToken();
        if (token is null)
            throw needsRelogin
                ? new UsageException("Claude login expired.", needsRelogin: true)
                // still signed in — Claude Code's short-lived token just can't refresh
                // right now (rate-limited). Recovers on its own; show saved usage meanwhile.
                : new UsageException("Usage temporarily unavailable.");

        using var request = new HttpRequestMessage(HttpMethod.Get, UsageUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
        request.Headers.TryAddWithoutValidation("User-Agent", "claude-code/2.1.202");

        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new UsageException("Rate limited by the usage endpoint.",
                response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.Now : null));
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UsageException("Token rejected.", needsRelogin: true);
        if (!response.IsSuccessStatusCode)
            throw new UsageException($"Usage endpoint returned HTTP {(int)response.StatusCode}.");

        var root = JsonNode.Parse(await response.Content.ReadAsStringAsync()) as JsonObject
            ?? throw new UsageException("Unexpected response from usage endpoint.");

        var windows = ParseWindows(root);
        if (windows.Count == 0)
            throw new UsageException("No usage windows in response.");

        return new UsageSnapshot(windows, DateTimeOffset.Now);
    }

    static List<UsageWindow> ParseWindows(JsonObject root)
    {
        var windows = new List<UsageWindow>();

        // modern shape: "limits" array — includes model-scoped weeklies (e.g. Fable)
        // that no longer appear as top-level fields
        if (root["limits"] is JsonArray limits)
        {
            foreach (var node in limits)
            {
                if (node is not JsonObject lim || lim["percent"] is null) continue;

                string kind = lim["kind"]?.GetValue<string>() ?? "unknown";
                string? model = lim["scope"]?["model"]?["display_name"]?.GetValue<string>();

                // keys stay compatible with the legacy shape so history carries over
                var (key, label) = kind switch
                {
                    "session" => ("five_hour", "Session (5h)"),
                    "weekly_all" => ("seven_day", "Weekly"),
                    "weekly_scoped" when model is not null =>
                        ("seven_day_" + model.ToLowerInvariant().Replace(' ', '_'), model + " Weekly"),
                    _ => (kind, Capitalize(kind.Replace('_', ' '))),
                };

                windows.Add(new UsageWindow(key, label,
                    lim["percent"]!.GetValue<double>(),
                    ParseResetTime(lim["resets_at"]),
                    lim["is_active"]?.GetValue<bool>() ?? false,
                    lim["severity"]?.GetValue<string>() ?? "normal"));
            }
        }

        // legacy shape fallback: top-level objects holding "utilization"
        if (windows.Count == 0)
        {
            foreach (var (key, value) in root)
            {
                if (key is "extra_usage" or "spend") continue; // handled by the merged-wallet block below
                if (value is not JsonObject obj || obj["utilization"] is null)
                    continue;
                windows.Add(new UsageWindow(key, LabelFor(key),
                    obj["utilization"]!.GetValue<double>(), ParseResetTime(obj["resets_at"])));
            }
        }

        // "extra_usage" and "spend" describe the same wallet (usage credits used vs
        // the monthly cap) — merge into one row instead of showing it twice. Emit it
        // when EITHER block is enabled; other accounts may carry only one.
        // a disabled block may still carry stale numbers, so only enabled blocks
        // contribute fields — otherwise the % and $ could come from different wallets
        var extra = root["extra_usage"] is JsonObject e && (e["is_enabled"]?.GetValue<bool>() ?? false) ? e : null;
        var spend = root["spend"] is JsonObject s && (s["enabled"]?.GetValue<bool>() ?? false) ? s : null;
        if (extra is not null || spend is not null)
        {
            var (usedDollars, limitDollars) = WalletDollars(extra, spend);

            // prefer extra_usage's decimal utilization over spend's rounded percent;
            // fall back to computing it from the dollar amounts if neither is present
            double? utilization = extra?["utilization"]?.GetValue<double>()
                ?? spend?["percent"]?.GetValue<double>()
                ?? (limitDollars is > 0 ? usedDollars / limitDollars * 100 : null);

            if (utilization is { } u)
                windows.Add(new UsageWindow("extra_usage", "Extra usage", u, null,
                    false, spend?["severity"]?.GetValue<string>() ?? "normal",
                    usedDollars, limitDollars));
        }

        // "Cloud credit": included credit for cloud sessions. Not in the limits array.
        // Codename field, matched by name only on purpose (decided in grilling 2026-10-02;
        // if renamed, the row just disappears — fix the one name).
        try
        {
            if (root["iguana_necktie"] is JsonObject cc
                && cc["limit_dollars"]?.GetValue<double>() is > 0 and var ccLimit
                && cc["used_dollars"]?.GetValue<double>() is { } ccUsed)
            {
                double ccUtil = cc["utilization"]?.GetValue<double>() ?? ccUsed / ccLimit * 100;
                windows.Add(new UsageWindow("cloud_credit", "Cloud credit", ccUtil,
                    ParseResetTime(cc["resets_at"]), false, "normal", ccUsed, ccLimit));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            // undocumented field changed type — lose the cloud row, never the whole poll
        }

        // Session first, plain weekly second, model-scoped after, wallet last.
        return windows
            .OrderBy(w => w.Key switch { "five_hour" => 0, "seven_day" => 1, "extra_usage" => 8, "cloud_credit" => 9, _ => 2 })
            .ThenBy(w => w.Label, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Dollar amounts for the merged wallet row. Prefers "spend" (minor units + exponent);
    /// falls back to "extra_usage" (used_credits/monthly_limit + decimal_places) — same
    /// numbers, different field names, since the two blocks describe one balance.
    /// </summary>
    static (double? used, double? limit) WalletDollars(JsonObject? extra, JsonObject? spend)
    {
        if (spend?["used"]?["amount_minor"] is JsonNode um && spend["limit"]?["amount_minor"] is JsonNode lm)
        {
            // each amount scales by its own exponent — they're 2/2 today, but nothing
            // guarantees the API keeps them in lockstep
            double usedDiv = Math.Pow(10, spend["used"]?["exponent"]?.GetValue<int>() ?? 2);
            double limitDiv = Math.Pow(10, spend["limit"]?["exponent"]?.GetValue<int>() ?? 2);
            return (um.GetValue<double>() / usedDiv, lm.GetValue<double>() / limitDiv);
        }
        if (extra?["used_credits"] is JsonNode uc && extra["monthly_limit"] is JsonNode ml)
        {
            double div = Math.Pow(10, extra["decimal_places"]?.GetValue<int>() ?? 2);
            return (uc.GetValue<double>() / div, ml.GetValue<double>() / div);
        }
        return (null, null);
    }

    static DateTimeOffset? ParseResetTime(JsonNode? node)
    {
        if (node is null) return null;
        var s = node.ToString();
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.ToLocalTime();
        if (long.TryParse(s, out var epoch))
            return DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime();
        return null;
    }

    static string LabelFor(string key) => key switch
    {
        "five_hour" => "Session (5h)",
        "seven_day" => "Weekly",
        _ when key.StartsWith("seven_day_", StringComparison.Ordinal) =>
            Capitalize(key["seven_day_".Length..].Replace('_', ' ')) + " Weekly",
        _ => Capitalize(key.Replace('_', ' ')),
    };

    static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
