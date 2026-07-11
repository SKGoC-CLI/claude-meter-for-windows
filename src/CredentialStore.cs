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
                Log.Warn($"token refresh failed: HTTP {(int)response.StatusCode} {Truncate(errorBody, 300)}");
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
            Log.Info($"token refresh OK, new expiry {DateTimeOffset.FromUnixTimeMilliseconds(token.ExpiresAtMs).ToLocalTime():HH:mm}");
            return token;
        }
        catch (Exception ex)
        {
            Log.Warn($"token refresh exception: {ex.Message}");
            return null;
        }
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
