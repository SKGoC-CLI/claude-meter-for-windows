using System.Text.Json.Nodes;

namespace ClaudeMeter;

/// <summary>
/// Provides a Claude OAuth access token from whichever local login is fresh:
/// the Claude Code CLI's credentials file, or — for people who work in the Claude
/// Desktop app and never touch the CLI — Desktop's own token cache
/// (<see cref="DesktopCredentialStore"/>). Both sources are strictly read-only:
/// we never refresh or rotate a token, so we can't invalidate anyone's login
/// (refresh-token rotation would risk a family revocation — anthropics/claude-code#54443).
/// When every source is expired we simply show the last-known usage as stale until
/// Claude Code (or Desktop) refreshes its own token the next time you use it.
/// </summary>
sealed class CredentialStore
{
    static readonly string CredentialsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");

    /// <summary>True when any local login exists (CLI file or Desktop cache).</summary>
    public bool HasAnyLogin => File.Exists(CredentialsPath) || DesktopCredentialStore.Exists;

    /// <summary>
    /// Outcome of a token request. <see cref="AccessToken"/> is null when we can't
    /// hand back a usable token; <see cref="NeedsRelogin"/> then says whether that is
    /// a real logged-out state (user must /login) or a transient one that recovers on
    /// its own (Claude Code will refresh its expired token the next time it runs).
    /// </summary>
    public readonly record struct TokenResult(string? AccessToken, bool NeedsRelogin);

    /// <summary>Returns the freshest available access token, or a reason none is usable.</summary>
    public TokenResult GetAccessToken()
    {
        // Prefer the CLI file when its token is live, otherwise fall back to Desktop's
        // (kept fresh by the Desktop app). Either is a valid key to the same account's usage.
        var fileToken = ReadFileToken();
        if (fileToken is { IsExpired: false })
            return new TokenResult(fileToken.AccessToken, NeedsRelogin: false);

        if (DesktopCredentialStore.TryRead() is { } dt &&
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= dt.ExpiresAtMs - 60_000)
            return new TokenResult(dt.AccessToken, NeedsRelogin: false);

        // Every source is expired or unreadable (a source may be mid-write). This is
        // transient from our read-only vantage point — Claude Code / Desktop refreshes its
        // own token next time you use it. We deliberately never raise "needs relogin" from
        // reading the stores, because a busy/torn file is indistinguishable from a revoked
        // one here; the authoritative logout signals live elsewhere and stay reliable: no
        // login at all (HasAnyLogin) and a 401/403 from the usage endpoint (UsageClient).
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
