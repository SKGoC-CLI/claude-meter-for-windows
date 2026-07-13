using System.Text.Json.Nodes;

namespace ClaudeMeter;

/// <summary>
/// Reads a Claude OAuth access token from Claude Code's credentials file.
/// The file is treated as strictly read-only: we never refresh or rotate the
/// token ourselves, so we can't invalidate Claude Code's own login (refresh-token
/// rotation would risk a token-family revocation — anthropics/claude-code#54443).
/// When the token has expired, Claude Code refreshes it the next time you use it;
/// until then the meter simply shows its last-known usage as stale.
/// </summary>
sealed class CredentialStore
{
    static readonly string CredentialsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");

    public bool CredentialsFileExists => File.Exists(CredentialsPath);

    /// <summary>
    /// Outcome of a token request. <see cref="AccessToken"/> is null when we can't
    /// hand back a usable token; <see cref="NeedsRelogin"/> then says whether that is
    /// a real logged-out state (user must /login) or a transient one that recovers on
    /// its own (Claude Code will refresh its expired token the next time it runs).
    /// </summary>
    public readonly record struct TokenResult(string? AccessToken, bool NeedsRelogin);

    /// <summary>Returns Claude Code's current access token, or a reason it is unavailable.</summary>
    public TokenResult GetAccessToken()
    {
        var fileToken = ReadFileToken();
        if (fileToken is { IsExpired: false })
            return new TokenResult(fileToken.AccessToken, NeedsRelogin: false);

        // Either the file couldn't be read/parsed (Claude Code may be mid-write), or the
        // token is expired. Both are transient from our read-only vantage point — Claude
        // Code refreshes the file the next time it runs. We deliberately never raise
        // "needs relogin" from the file alone, because a busy/torn file is indistinguishable
        // from a revoked one here; the authoritative logout signals live elsewhere and stay
        // reliable: a missing file (CredentialsFileExists) and a 401/403 from the usage
        // endpoint (UsageClient). So an expired/unreadable file just shows stale usage.
        return new TokenResult(null, NeedsRelogin: false);
    }

    sealed record TokenInfo(string AccessToken, long ExpiresAtMs)
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
            return new TokenInfo(access, oauth?["expiresAt"]?.GetValue<long>() ?? 0);
        }
        catch
        {
            return null;
        }
    }
}
