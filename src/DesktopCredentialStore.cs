using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ClaudeMeter;

/// <summary>
/// Reads the OAuth access token that the Claude Desktop app keeps in its own
/// encrypted token cache (Chromium "os_crypt v10": AES-256-GCM under a DPAPI-
/// wrapped key). Desktop keeps this token fresh as you use it, so it fills the
/// gap for users who work in the Desktop app's Code tab and never run the Claude
/// Code CLI — whose <c>~/.claude/.credentials.json</c> then goes stale.
/// Strictly read-only and Windows/CurrentUser-scoped: we never write or refresh.
/// </summary>
static class DesktopCredentialStore
{
    static readonly string DataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude");
    static readonly string ConfigPath = Path.Combine(DataDir, "config.json");
    static readonly string LocalStatePath = Path.Combine(DataDir, "Local State");

    public static bool Exists => File.Exists(ConfigPath) && File.Exists(LocalStatePath);

    /// <summary>Access token + its expiry (unix ms), or null when unavailable/undecryptable.</summary>
    // Decrypting on every 180s poll is wasteful; Desktop rewrites config.json only when
    // it rotates the token, so we re-decrypt only when the file's timestamp changes.
    static (string AccessToken, long ExpiresAtMs)? _cached;
    static DateTime _cachedStampUtc;

    public static (string AccessToken, long ExpiresAtMs)? TryRead()
    {
        try
        {
            var stamp = File.GetLastWriteTimeUtc(ConfigPath);
            if (_cached is { } c && stamp == _cachedStampUtc)
                return c;

            var config = JsonNode.Parse(File.ReadAllText(ConfigPath));
            string? cache = config?["oauth:tokenCacheV2"]?.GetValue<string>()
                         ?? config?["oauth:tokenCache"]?.GetValue<string>();
            if (string.IsNullOrEmpty(cache)) return null;

            byte[] plain = DecryptOsCrypt(Convert.FromBase64String(cache));
            var json = JsonNode.Parse(Encoding.UTF8.GetString(plain));

            var (access, expiresAtMs) = FindToken(json);
            if (string.IsNullOrEmpty(access)) return null;

            // If we couldn't find an expiry, assume it's live — Desktop keeps it fresh,
            // and a genuinely stale token is caught by the usage endpoint (401/403).
            var result = (access!, expiresAtMs <= 0 ? long.MaxValue : expiresAtMs);
            _cached = result;
            _cachedStampUtc = stamp;
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn($"desktop token read failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Decrypts a Chromium os_crypt blob ("v10"/"v11" AES-GCM, else legacy DPAPI).</summary>
    static byte[] DecryptOsCrypt(byte[] blob)
    {
        // layout: "v10"(3) + nonce(12) + ciphertext + tag(16) => min 31 bytes
        if (blob.Length >= 31 && blob[0] == (byte)'v' && blob[1] == (byte)'1' &&
            (blob[2] == (byte)'0' || blob[2] == (byte)'1'))
        {
            byte[] key = GetMasterKey();
            var nonce = blob.AsSpan(3, 12);
            var tag = blob.AsSpan(blob.Length - 16, 16);
            var cipher = blob.AsSpan(15, blob.Length - 15 - 16);
            var outBuf = new byte[cipher.Length];
            using var gcm = new AesGcm(key, 16);
            gcm.Decrypt(nonce, cipher, tag, outBuf);
            return outBuf;
        }
        // pre-v10 apps stored a raw DPAPI blob
        return Dpapi.Unprotect(blob);
    }

    /// <summary>The AES key from Local State: base64, "DPAPI"-prefixed, DPAPI-wrapped.</summary>
    static byte[] GetMasterKey()
    {
        var ls = JsonNode.Parse(File.ReadAllText(LocalStatePath));
        string b64 = ls?["os_crypt"]?["encrypted_key"]?.GetValue<string>()
            ?? throw new InvalidOperationException("no os_crypt.encrypted_key in Local State");
        byte[] wrapped = Convert.FromBase64String(b64);
        return Dpapi.Unprotect(wrapped.AsSpan(5).ToArray()); // strip the "DPAPI" prefix
    }

    /// <summary>
    /// Finds the freshest access token in the decrypted cache. Desktop stores one entry
    /// per account/client (keyed by "clientId:accountId:host:scopes"), each holding
    /// <c>token</c> + <c>expiresAt</c>; we pair a token with its own entry's expiry and
    /// keep the one that lives longest. Tolerant of the exact shape so a Desktop update
    /// is less likely to break it.
    /// </summary>
    static (string?, long) FindToken(JsonNode? root)
    {
        (string token, long exp)? best = null;

        void Walk(JsonNode? n)
        {
            switch (n)
            {
                case JsonObject o:
                    string? token = null;
                    long exp = 0;
                    bool looksLikeTokenEntry = false; // has an expiry or a refresh-token sibling
                    foreach (var (k, v) in o)
                    {
                        if (v is not JsonValue jv) continue;
                        string key = k.ToLowerInvariant();
                        if (key is "token" or "accesstoken" or "access_token") token = TryString(jv);
                        else if (key is "expiresat" or "expires_at") { exp = ReadEpochMs(jv); looksLikeTokenEntry = true; }
                        else if (key is "refreshtoken" or "refresh_token") looksLikeTokenEntry = true;
                    }
                    // require corroborating fields so a stray "token" property elsewhere in the
                    // blob can't masquerade as the OAuth access token
                    if (token is { Length: > 0 } && looksLikeTokenEntry && (best is null || exp > best.Value.exp))
                        best = (token, exp);
                    // recurse for sibling/nested entries
                    foreach (var (_, v) in o) if (v is JsonObject or JsonArray) Walk(v);
                    break;
                case JsonArray a:
                    foreach (var e in a) Walk(e);
                    break;
            }
        }

        Walk(root);
        return best is { } b ? (b.token, b.exp) : (null, 0);
    }

    static string? TryString(JsonNode n)
    {
        try { return n.GetValue<string>(); } catch { return null; }
    }

    /// <summary>Reads an expiry given as unix seconds, unix ms, or an ISO-8601 string.</summary>
    static long ReadEpochMs(JsonNode n)
    {
        var s = n.ToString();
        if (long.TryParse(s, out var num))
            return num > 100_000_000_000L ? num : num * 1000; // ms vs seconds
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.ToUnixTimeMilliseconds();
        return 0;
    }

    /// <summary>DPAPI CryptUnprotectData (CurrentUser) via P/Invoke — no extra package needed.</summary>
    static class Dpapi
    {
        public static byte[] Unprotect(byte[] data)
        {
            var inBlob = new DATA_BLOB();
            var outBlob = new DATA_BLOB();
            var hIn = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                inBlob.cbData = data.Length;
                inBlob.pbData = hIn.AddrOfPinnedObject();
                if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref outBlob))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            finally
            {
                hIn.Free();
                if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
            IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll")]
        static extern IntPtr LocalFree(IntPtr hMem);
    }
}
