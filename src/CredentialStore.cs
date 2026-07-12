using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudeMeter;

/// <summary>
/// Provides a valid Claude OAuth access token. Primary source is Claude Code's
/// credentials file; if that token is expired we refresh it in-memory (never
/// writing back to Claude Code's file) and cache our copy under %APPDATA%.
/// </summary>
sealed class CredentialStore
{
    const string OAuthClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e"; // Claude Code public client
    const string TokenUrl = "https://platform.claude.com/v1/oauth/token"; // moved from console.anthropic.com

    static readonly string CredentialsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");

    static readonly string CachePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeMeter", "token.json");

    readonly HttpClient _http;

    // polite retry: the refresh endpoint rate-limits hard, so back off on failure
    static readonly TimeSpan InitialBackoff = TimeSpan.FromMinutes(10);
    static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(2);
    TimeSpan _backoff = TimeSpan.Zero;
    DateTimeOffset _nextRefreshAllowed = DateTimeOffset.MinValue;
    string? _lastFailedRefreshToken;

    public CredentialStore(HttpClient http) => _http = http;

    public bool CredentialsFileExists => File.Exists(CredentialsPath);

    /// <summary>Returns a non-expired access token, or null when unavailable.</summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        var fileToken = ReadFileToken();
        if (fileToken is { IsExpired: false })
            return fileToken.AccessToken;

        // Claude Code's token is expired (or file missing) — try our own cached refresh.
        var cached = ReadCachedToken();
        if (cached is { IsExpired: false })
            return cached.AccessToken;

        var refreshToken = cached?.RefreshToken ?? fileToken?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            Log.Warn("no refresh token available (file token expired, no usable cache)");
            return null;
        }

        // a different refresh token means a fresh login — worth trying immediately
        if (refreshToken != _lastFailedRefreshToken)
        {
            _backoff = TimeSpan.Zero;
            _nextRefreshAllowed = DateTimeOffset.MinValue;
        }

        if (DateTimeOffset.Now < _nextRefreshAllowed)
            return null; // cooling down after a failed attempt

        Log.Info($"file token expired; attempting refresh (using {(cached?.RefreshToken is not null ? "cached" : "file")} refresh token)");
        var refreshed = await RefreshAsync(refreshToken);
        return refreshed?.AccessToken;
    }

    sealed record TokenInfo(string AccessToken, string? RefreshToken, long ExpiresAtMs)
    {
        // 60s safety margin so we never present a token that dies mid-request
        public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > ExpiresAtMs - 60_000;
    }

    TokenInfo? ReadFileToken()
    {
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(CredentialsPath));
            var oauth = root?["claudeAiOauth"];
            var access = oauth?["accessToken"]?.GetValue<string>();
            if (string.IsNullOrEmpty(access)) return null;
            return new TokenInfo(
                access,
                oauth?["refreshToken"]?.GetValue<string>(),
                oauth?["expiresAt"]?.GetValue<long>() ?? 0);
        }
        catch
        {
            return null;
        }
    }

    TokenInfo? ReadCachedToken()
    {
        try
        {
            return JsonSerializer.Deserialize<TokenInfo>(File.ReadAllText(CachePath));
        }
        catch
        {
            return null;
        }
    }

    async Task<TokenInfo?> RefreshAsync(string refreshToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = JsonContent.Create(new
                {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken,
                    client_id = OAuthClientId,
                }),
            };
            // without the claude-code UA this endpoint rate-limits aggressively
            request.Headers.TryAddWithoutValidation("User-Agent", "claude-code/2.1.202");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                ApplyBackoff(refreshToken, response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.Now : null));
                Log.Warn($"token refresh failed: HTTP {(int)response.StatusCode} {Truncate(errorBody, 300)}; next attempt after {_nextRefreshAllowed:HH:mm}");
                return null;
            }

            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var access = body?["access_token"]?.GetValue<string>();
            if (string.IsNullOrEmpty(access)) return null;

            var expiresIn = body?["expires_in"]?.GetValue<long>() ?? 3600;
            var token = new TokenInfo(
                access,
                body?["refresh_token"]?.GetValue<string>() ?? refreshToken,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + expiresIn * 1000);

            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            File.WriteAllText(CachePath, JsonSerializer.Serialize(token));
            _backoff = TimeSpan.Zero;
            _nextRefreshAllowed = DateTimeOffset.MinValue;
            _lastFailedRefreshToken = null;
            Log.Info($"token refresh OK, new expiry {DateTimeOffset.FromUnixTimeMilliseconds(token.ExpiresAtMs).ToLocalTime():HH:mm}");
            return token;
        }
        catch (Exception ex)
        {
            ApplyBackoff(refreshToken, retryAfter: null);
            Log.Warn($"token refresh exception: {ex.Message}; next attempt after {_nextRefreshAllowed:HH:mm}");
            return null;
        }
    }

    /// <summary>Honors Retry-After when given, otherwise doubles the wait (10 m → … → 2 h cap).</summary>
    void ApplyBackoff(string refreshToken, TimeSpan? retryAfter)
    {
        _backoff = retryAfter is { } ra && ra > TimeSpan.Zero
            ? (ra < MaxBackoff ? ra : MaxBackoff)
            : (_backoff == TimeSpan.Zero
                ? InitialBackoff
                : TimeSpan.FromTicks(Math.Min(_backoff.Ticks * 2, MaxBackoff.Ticks)));
        _nextRefreshAllowed = DateTimeOffset.Now + _backoff;
        _lastFailedRefreshToken = refreshToken;
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
