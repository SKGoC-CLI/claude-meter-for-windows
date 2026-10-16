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
    double? LimitDollars = null,
    int? ResetsLeft = null,      // limit-resets row only: free resets left / granted, over live grants
    int? ResetsTotal = null,
    int ResetGrants = 0)
{
    /// <summary>Small-line rows (cloud credit, usage-credit balance, limit resets) — never a
    /// blocking limit, so kept out of the bars, tray icon, alerts and ETA.</summary>
    public bool IsWallet => Key is "cloud_credit" or "credit_balance" or "limit_resets";
}

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
    // cedar_ember=1 adds the limit-resets block (null without it), as Claude Code asks for it;
    // never skip_spend=1 like Claude Code does — the Monthly spend row needs that block
    const string UsageUrl = "https://api.anthropic.com/api/oauth/usage?cedar_ember=1";

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

        string body = await response.Content.ReadAsStringAsync();
        // latest raw response, overwritten each poll — the only way to see undocumented
        // fields without touching the token (holds usage numbers only, no secrets)
        try { Directory.CreateDirectory(Log.Dir); File.WriteAllText(Path.Combine(Log.Dir, "usage-raw.json"), body); } catch (Exception) { }

        var root = JsonNode.Parse(body) as JsonObject
            ?? throw new UsageException("Unexpected response from usage endpoint.");

        var windows = ParseWindows(root);
        if (windows.Count == 0)
            throw new UsageException("No usage windows in response.");

        // spend.balance stays null even with purchased credits — the balance lives on its
        // own endpoint (sorts last, so appending keeps the order)
        if (!windows.Any(w => w.Key == "credit_balance") && await FetchPrepaidDollarsAsync(token) is { } prepaid)
            windows.Add(new UsageWindow("credit_balance", "Usage credits", 0, null, false, "normal", prepaid));

        return new UsageSnapshot(windows, DateTimeOffset.Now);
    }

    const string ProfileUrl = "https://api.anthropic.com/api/oauth/profile";
    const string PrepaidUrl = "https://api.anthropic.com/api/oauth/organizations/{0}/prepaid/credits";
    string? _orgUuid; // from the profile once; cleared on a failed balance call (re-login, org switch)
    double? _lastPrepaid; // kept through a failed call so the row doesn't flicker away
    DateTimeOffset _prepaidRetryAt; // back-off: a failing (or unsupported) account isn't asked every poll

    /// <summary>Purchased usage-credit balance (Settings → Usage → "Usage credits"), via the
    /// same call Claude Code makes for it. Never fails the poll: on any failure it keeps the
    /// last known value and waits 30 min before asking again.</summary>
    async Task<double?> FetchPrepaidDollarsAsync(string token)
    {
        if (DateTimeOffset.Now < _prepaidRetryAt) return _lastPrepaid;
        try
        {
            // the profile also holds name/email — read the org id only, never dump it
            _orgUuid ??= (await GetJsonAsync(ProfileUrl, token, null, null))?["organization"]?["uuid"]?.GetValue<string>();
            var data = _orgUuid is null ? null
                : await GetJsonAsync(string.Format(PrepaidUrl, Uri.EscapeDataString(_orgUuid)), token, _orgUuid, "prepaid-raw.json");
            // amount is in cents (Claude Code's own mock fills it from balanceCents)
            if (data?["amount"] is JsonValue a && a.TryGetValue<double>(out var cents))
                return _lastPrepaid = cents / 100;
            _orgUuid = null;
        }
        catch (Exception ex) // anything — the usage numbers already in hand must still ship
        {
            Log.Warn($"prepaid balance unavailable: {ex.Message}");
        }
        _prepaidRetryAt = DateTimeOffset.Now.AddMinutes(30);
        return _lastPrepaid;
    }

    async Task<JsonObject?> GetJsonAsync(string url, string token, string? orgUuid, string? dumpName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
        request.Headers.TryAddWithoutValidation("User-Agent", "claude-code/2.1.202");
        if (orgUuid is not null) request.Headers.TryAddWithoutValidation("x-organization-uuid", orgUuid);

        // shorter than the client's 30 s: these extras must not hold back the usage numbers
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var response = await _http.SendAsync(request, cts.Token);
        string body = await response.Content.ReadAsStringAsync(cts.Token);
        if (dumpName is not null)
            try { File.WriteAllText(Path.Combine(Log.Dir, dumpName), $"HTTP {(int)response.StatusCode}\n{body}"); } catch (Exception) { }
        if (!response.IsSuccessStatusCode)
        {
            Log.Warn($"{new Uri(url).AbsolutePath.Split('/').Last()} returned HTTP {(int)response.StatusCode}");
            return null;
        }
        return JsonNode.Parse(body) as JsonObject;
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
                windows.Add(new UsageWindow("extra_usage", "Monthly spend", u, null,
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

        // "Usage credits": prepaid spend.balance, shown even when the spend toggle is off
        // (the spend block itself is the "Monthly spend" row above, as Settings names it).
        // Shape assumed like spend.used (amount_minor + exponent); a plain number = dollars.
        try
        {
            double? balance = root["spend"]?["balance"] switch
            {
                JsonObject b when b["amount_minor"] is JsonNode m =>
                    m.GetValue<double>() / Math.Pow(10, b["exponent"]?.GetValue<int>() ?? 2),
                JsonValue v => v.GetValue<double>(),
                _ => null
            };
            if (balance is { } bal)
                windows.Add(new UsageWindow("credit_balance", "Usage credits", 0, null, false, "normal", bal));
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            // unknown balance shape — lose this row only
        }

        // "Limit resets": free resets of a hit limit, in grants (codename cedar_ember).
        // Only grants with resets left and not yet expired count; none = no row.
        try
        {
            if (root["cedar_ember"]?["grants"] is JsonArray grants)
            {
                var now = DateTimeOffset.Now;
                var live = grants.OfType<JsonObject>()
                    // read as double: GetValue<int> throws on "2.0"
                    .Select(g => (Left: (int)(g["resets_left"]?.GetValue<double>() ?? 0),
                                  Total: (int)(g["resets_total"]?.GetValue<double>() ?? 0),
                                  Ends: ParseResetTime(g["ends_at"])))
                    .Where(g => g.Left > 0 && (g.Ends is null || g.Ends > now))
                    .ToList();
                if (live.Count > 0)
                    windows.Add(new UsageWindow("limit_resets", "Limit resets", 0, live.Min(g => g.Ends),
                        ResetsLeft: live.Sum(g => g.Left), ResetsTotal: live.Sum(g => Math.Max(g.Total, g.Left)),
                        ResetGrants: live.Count));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            // undocumented block changed shape — lose this row only
        }

        // Session first, plain weekly second, model-scoped after, wallet last.
        return windows
            .OrderBy(w => w.Key switch { "five_hour" => 0, "seven_day" => 1, "extra_usage" => 8, "cloud_credit" => 9, "credit_balance" => 10, "limit_resets" => 11, _ => 2 })
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
