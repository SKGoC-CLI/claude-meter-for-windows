# Dev notes — how Claude Meter gets a usage token (v1.5.0)

_Written 2026-07-14 while fixing the recurring "Claude login expired" popup._

## TL;DR

- The usage numbers from `/api/oauth/usage` are **account-wide** — they already
  include everything you spend via CLI, Desktop, or web. The meter only needs
  **any fresh token for your account** to read them.
- The old pain: the meter read only the **Claude Code CLI** token
  (`~/.claude/.credentials.json`). It expires every ~8 h, and if you don't use the
  CLI, nothing refreshes it — so the meter went dark and showed a scary red
  **"Claude login expired"** even though you were still signed in.
- v1.5.0 fixes this three ways: (1) never mislabel a temporary problem as a
  logout, (2) never refresh a token ourselves (read-only), (3) also read the
  **Claude Desktop** login, which stays fresh as you use the Desktop Code tab.

## Root cause (two layers)

### 1. False "login expired"
When the CLI token was expired the meter tried to refresh it against
`https://platform.claude.com/v1/oauth/token`. That endpoint **rate-limits hard**
(persistent HTTP 429, ~2 h `Retry-After`), so the refresh never succeeded — and
any failure was surfaced as a red "login expired" + "Fix Claude login" button.
But a rate-limited refresh is **not** a logout; the login was still valid.

### 2. Wrong token source for a Desktop user
The real reason the token stayed stale: **this machine uses the Claude Desktop
app's Code tab, not the CLI.** The two keep **separate** logins. Desktop never
touches `~/.claude/.credentials.json`, so that file sat frozen from the morning's
last CLI login (8 h token) and expired at midday — while Desktop kept working fine
with its own, continuously-refreshed token.

## The fix

### Design principle: every login source is strictly read-only
The meter no longer refreshes or rotates any token. This:
- eliminates all traffic to the rate-limited token endpoint, and
- removes the risk that our refresh rotates the shared refresh token and trips
  server-side reuse-detection, revoking Claude Code's whole token family and
  forcing a real `/login` (see anthropics/claude-code#54443).

Trade-off: when every login source is stale (e.g. Claude fully closed for hours),
the meter shows its last-known usage as amber **"stale"** until you next use
Claude, at which point that app refreshes its own token and we pick it up.

### "Needs re-login" is raised only by reliable signals
Reading a token store can't distinguish a revoked login from a busy/torn file, so
we never raise a logout from the read itself. The red "login expired" view is
shown only when:
- **no login exists at all** (`CredentialStore.HasAnyLogin` is false), or
- the **usage endpoint returns 401/403** (the token was actually rejected).

Everything else (expired token, unreadable file, network blip, rate limit) is
**transient** → show last-known usage as stale, no alarm, no "Fix login" button.

### Two token sources, freshest wins
`CredentialStore.GetAccessToken()`:
1. CLI file token (`~/.claude/.credentials.json` → `claudeAiOauth.accessToken`)
   if not expired, else
2. Claude Desktop token (see below) if not expired, else
3. `(null, NeedsRelogin: false)` → transient/stale.

## How the Claude Desktop token is read (`DesktopCredentialStore`)

Desktop is an Electron app; its OAuth tokens live in its config, encrypted with
the standard Chromium `os_crypt` scheme. All of this is **read-only** and only
works for the **same Windows user** (DPAPI is user-scoped).

1. **Cache blob** — `%APPDATA%\Claude\config.json`, key `oauth:tokenCacheV2`
   (legacy `oauth:tokenCache`). Base64 of a blob prefixed **`v10`** →
   `[ "v10"(3) | nonce(12) | ciphertext | GCM tag(16) ]`, AES-256-GCM.
2. **Master key** — `%APPDATA%\Claude\Local State`, key
   `os_crypt.encrypted_key`. Base64, prefixed **`DPAPI`**; strip the 5-byte
   prefix and `CryptUnprotectData` (DPAPI, CurrentUser) → the 32-byte AES key.
   (DPAPI is done via a small `crypt32.dll` P/Invoke so we add no NuGet package.)
3. **Decrypt & parse** — AES-GCM-decrypt the cache to JSON. It holds one entry
   per account/client, keyed `clientId:accountId:host:scopes`, each:
   `{ token, refreshToken, expiresAt, subscriptionType, rateLimitTier }`.
   The access token is under **`token`** (not `accessToken`). We take the entry
   with the furthest-out `expiresAt` (freshest), requiring a corroborating
   `expiresAt`/`refreshToken` sibling so a stray `token` field can't be mistaken
   for it.
4. **Cache by mtime** — Desktop rewrites `config.json` only when it rotates the
   token, so we re-decrypt only when the file's timestamp changes (not every poll).

Verified end-to-end: after wiring this in, the meter logged
`usage ok: Session (5h) 94% …` from the Desktop token — matching real Desktop use
(the stale CLI snapshot had it at 16%).

## Known limitation

If you are signed into **two different accounts** — one in the CLI, another in
Desktop — the meter shows whichever token is currently live. Fine for
single-account use, which is the norm for a personal tool.

## Files

| File | Role |
| --- | --- |
| `src/CredentialStore.cs` | Read-only CLI reader + chooses CLI/Desktop source; `HasAnyLogin` |
| `src/DesktopCredentialStore.cs` | Decrypts & reads the Claude Desktop token (DPAPI + AES-GCM) |
| `src/UsageClient.cs` | Calls the usage endpoint; owns the reliable logout signals (401/403, no login) |
| `src/PopupForm.cs` | Red "login expired" only when `NeedsRelogin`; amber "stale" otherwise |

## Released

**v1.5.0** — 2026-07-14 —
https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.5.0
(commits `36cc1ba`, `4117a3e`, `d84adc4`).
