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
    string Severity = "normal");

sealed record UsageSnapshot(IReadOnlyList<UsageWindow> Windows, DateTimeOffset FetchedAt);

sealed class UsageException : Exception
{
    public TimeSpan? RetryAfter { get; }
    public UsageException(string message, TimeSpan? retryAfter = null) : base(message) => RetryAfter = retryAfter;
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
        if (!_credentials.CredentialsFileExists)
            throw new UsageException("Claude Code is not logged in.");

        var token = await _credentials.GetAccessTokenAsync()
            ?? throw new UsageException("Claude login expired.");

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
            throw new UsageException("Token rejected.");
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
                if (key == "extra_usage") continue; // handled by the dedicated block below
                if (value is not JsonObject obj || obj["utilization"] is null)
                    continue;
                windows.Add(new UsageWindow(key, LabelFor(key),
                    obj["utilization"]!.GetValue<double>(), ParseResetTime(obj["resets_at"])));
            }
        }

        // optional wallets, shown only when enabled on the account
        if (root["extra_usage"] is JsonObject extra &&
            (extra["is_enabled"]?.GetValue<bool>() ?? false) &&
            extra["utilization"] is not null)
        {
            windows.Add(new UsageWindow("extra_usage", "Extra usage",
                extra["utilization"]!.GetValue<double>(), null));
        }

        if (root["spend"] is JsonObject spend &&
            (spend["enabled"]?.GetValue<bool>() ?? false) &&
            spend["percent"] is not null)
        {
            windows.Add(new UsageWindow("spend", "Spend",
                spend["percent"]!.GetValue<double>(), null,
                false, spend["severity"]?.GetValue<string>() ?? "normal"));
        }

        // Session first, plain weekly second, model-scoped after, wallets last.
        return windows
            .OrderBy(w => w.Key switch { "five_hour" => 0, "seven_day" => 1, "extra_usage" => 8, "spend" => 9, _ => 2 })
            .ThenBy(w => w.Label, StringComparer.Ordinal)
            .ToList();
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
