using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace ClaudeMeter;

sealed record UsageWindow(string Key, string Label, double Utilization, DateTimeOffset? ResetsAt);

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
            throw new UsageException("Claude Code is not logged in.\nRun 'claude' and sign in first.");

        var token = await _credentials.GetAccessTokenAsync()
            ?? throw new UsageException("Claude login expired.\nRun 'claude' in a terminal and /login once.");

        using var request = new HttpRequestMessage(HttpMethod.Get, UsageUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
        request.Headers.TryAddWithoutValidation("User-Agent", "claude-code/2.1.202");

        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new UsageException("Rate limited by the usage endpoint.", response.Headers.RetryAfter?.Delta);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UsageException("Token rejected.\nRun 'claude' in a terminal and /login once.");
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
        foreach (var (key, value) in root)
        {
            if (value is not JsonObject obj || obj["utilization"] is null)
                continue;

            double utilization = obj["utilization"]!.GetValue<double>();

            DateTimeOffset? resetsAt = null;
            var resetsRaw = obj["resets_at"];
            if (resetsRaw is not null)
            {
                var s = resetsRaw.ToString();
                if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
                    resetsAt = dto.ToLocalTime();
                else if (long.TryParse(s, out var epoch))
                    resetsAt = DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime();
            }

            windows.Add(new UsageWindow(key, LabelFor(key), utilization, resetsAt));
        }

        // Session first, plain weekly second, model-specific weeklies after.
        return windows
            .OrderBy(w => w.Key switch { "five_hour" => 0, "seven_day" => 1, _ => 2 })
            .ThenBy(w => w.Label, StringComparer.Ordinal)
            .ToList();
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
