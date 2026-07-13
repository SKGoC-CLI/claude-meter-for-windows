using System.Net;
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
    readonly HashSet<string> _failedRefreshTokens = new();

    // Was the most recent refresh failure a genuine auth rejection (revoked/invalid
    // refresh token) rather than a transient rate-limit/network hiccup? Only the
    // former means the user actually has to sign in again.
    bool _lastRefreshFatal;

    public CredentialStore(HttpClient http) => _http = http;

    public bool CredentialsFileExists => File.Exists(CredentialsPath);

    /// <summary>
    /// Outcome of a token request. <see cref="AccessToken"/> is null when we can't
    /// hand back a usable token; <see cref="NeedsRelogin"/> then says whether that is
    /// a real logged-out state (user must /login) or a transient one that recovers on
    /// its own (rate-limited refresh, network blip — the saved login is still valid).
    /// </summary>
    public readonly record struct TokenResult(string? AccessToken, bool NeedsRelogin);

    /// <summary>Returns a non-expired access token, or a reason it is unavailable.</summary>
    public async Task<TokenResult> GetAccessTokenAsync()
    {
        var fileToken = ReadFileToken();
        if (fileToken is { IsExpired: false })
            return new TokenResult(fileToken.AccessToken, NeedsRelogin: false);

        // Claude Code's token is expired (or file missing) — try our own cached refresh.
        var cached = ReadCachedToken();
        if (cached is { IsExpired: false })
            return new TokenResult(cached.AccessToken, NeedsRelogin: false);

        // Our cache may hold a rotated token newer than Claude Code's file, but a
        // fresh /login in Claude Code revokes our family — so keep both as candidates
        // and never let a dead cached token permanently shadow the file's.
        var candidates = new List<string>();
        if (cached?.RefreshToken is { Length: > 0 } cachedRt) candidates.Add(cachedRt);
        if (fileToken?.RefreshToken is { Length: > 0 } fileRt && !candidates.Contains(fileRt)) candidates.Add(fileRt);
        if (candidates.Count == 0)
        {
            // No refresh token anywhere: a stored login this broken needs a real /login.
            Log.Warn("no refresh token available (file token expired, no usable cache)");
            return new TokenResult(null, NeedsRelogin: true);
        }

        // an untried token (fresh login or rotation) is worth trying immediately
        var refreshToken = candidates.Find(t => !_failedRefreshTokens.Contains(t));
        if (refreshToken is null)
        {
            if (DateTimeOffset.Now < _nextRefreshAllowed)
                // every candidate failed recently — cooling down. Carry the last verdict
                // so a rate-limited wait stays "transient", not a false "login expired".
                return new TokenResult(null, NeedsRelogin: _lastRefreshFatal);
            _failedRefreshTokens.Clear(); // backoff elapsed; give them another chance
            refreshToken = candidates[0];
        }

        Log.Info($"file token expired; attempting refresh (using {(refreshToken == cached?.RefreshToken ? "cached" : "file")} refresh token)");
        var refreshed = await RefreshAsync(refreshToken);
        if (refreshed is not null)
            return new TokenResult(refreshed.AccessToken, NeedsRelogin: false);
        return new TokenResult(null, NeedsRelogin: _lastRefreshFatal);
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
                // A 400/401 (typically invalid_grant) means the refresh token is dead —
                // a fresh /login is the only cure. A 429 or 5xx is transient: the saved
                // login is still valid and will recover once the endpoint lets us in.
                _lastRefreshFatal =
                    response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                    || errorBody.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase);
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
            _failedRefreshTokens.Clear();
            _lastRefreshFatal = false;
            Log.Info($"token refresh OK, new expiry {DateTimeOffset.FromUnixTimeMilliseconds(token.ExpiresAtMs).ToLocalTime():HH:mm}");
            return token;
        }
        catch (Exception ex)
        {
            _lastRefreshFatal = false; // a network/timeout blip is transient, not a logout
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
        _failedRefreshTokens.Add(refreshToken);
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
