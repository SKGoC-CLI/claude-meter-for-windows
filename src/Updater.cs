using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace ClaudeMeter;

sealed record ReleaseInfo(Version Version, string Tag, string PageUrl, string? ZipUrl, string? Sha256);

/// <summary>
/// Self-update (ADR 0003): download the release zip from GitHub, check it against the
/// sha256 digest GitHub publishes, check the exe's file version equals the tag, then
/// rename the running exe to ClaudeMeter.old.exe, move the new one into its place and
/// start it with --updated. The new process deletes the .old.exe on startup.
/// </summary>
static class Updater
{
    const string Repo = "SKGoC-CLI/claude-meter-for-windows";
    const string LatestReleaseApi = $"https://api.github.com/repos/{Repo}/releases/latest";
    const string DownloadPrefix = $"https://github.com/{Repo}/releases/download/";
    const string AssetName = "ClaudeMeter-portable.zip";
    const string ExeName = "ClaudeMeter.exe";
    const long MaxZipBytes = 200L * 1024 * 1024;
    public const string UpdatedArg = "--updated";

    static string TempDir => Path.Combine(Path.GetTempPath(), "ClaudeMeter-update");

    public static Version Current
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
        }
    }

    public static async Task<ReleaseInfo?> GetLatestAsync(HttpClient http)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.TryAddWithoutValidation("User-Agent", "ClaudeMeter");
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null; // repo or release may not exist yet

        var root = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        string? tag = root?["tag_name"]?.GetValue<string>();
        string? page = root?["html_url"]?.GetValue<string>();
        if (tag is null || page is null) return null;
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var v)) return null;

        string? zipUrl = null, sha = null;
        if (root?["assets"] is JsonArray assets)
        {
            foreach (var a in assets)
            {
                if (a?["name"]?.GetValue<string>() != AssetName) continue;
                zipUrl = a["browser_download_url"]?.GetValue<string>();
                string? digest = a["digest"]?.GetValue<string>();
                if (digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    sha = digest["sha256:".Length..].Trim();
                break;
            }
        }
        return new ReleaseInfo(new Version(v.Major, v.Minor, Math.Max(v.Build, 0)), tag, page, zipUrl, sha);
    }

    /// <summary>
    /// Downloads, verifies and swaps the exe, then starts the new one. On return the caller
    /// must exit this process. Throws InvalidOperationException with a user-facing message
    /// on any failure, leaving the installed exe untouched.
    /// </summary>
    public static async Task InstallAsync(ReleaseInfo release, IProgress<int> progress)
    {
        if (release.ZipUrl is null || !release.ZipUrl.StartsWith(DownloadPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("The release has no downloadable package.");
        if (string.IsNullOrEmpty(release.Sha256))
            throw new InvalidOperationException("GitHub did not publish a checksum for this release.");

        string target = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the running app.");
        string dir = Path.GetDirectoryName(target)!;
        if (!IsWritable(dir))
            throw new InvalidOperationException($"The app folder is not writable:\n{dir}");

        Directory.CreateDirectory(TempDir);
        string zipPath = Path.Combine(TempDir, AssetName);
        string newExe = Path.Combine(TempDir, "ClaudeMeter.new.exe");

        // own client: the shared one has a 30 s timeout, too short for ~65 MB on a slow line
        using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
        {
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ClaudeMeter");
            using var response = await http.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Download failed (HTTP {(int)response.StatusCode}).");
            long? total = response.Content.Headers.ContentLength;
            if (total > MaxZipBytes) throw new InvalidOperationException("The download is unexpectedly large.");

            await using var src = await response.Content.ReadAsStreamAsync();
            await using var dst = File.Create(zipPath);
            var buffer = new byte[81920];
            long done = 0;
            int read, lastPct = -1;
            while ((read = await src.ReadAsync(buffer)) > 0)
            {
                done += read;
                if (done > MaxZipBytes) throw new InvalidOperationException("The download is unexpectedly large.");
                await dst.WriteAsync(buffer.AsMemory(0, read));
                if (total > 0)
                {
                    int pct = (int)(done * 100 / total.Value);
                    if (pct != lastPct) progress.Report(lastPct = pct);
                }
            }
        }

        string actual;
        await using (var fs = File.OpenRead(zipPath))
            actual = Convert.ToHexString(await SHA256.HashDataAsync(fs));
        if (!actual.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The download is corrupted (checksum mismatch).");

        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var entry = zip.Entries.FirstOrDefault(e => e.FullName == ExeName)
                ?? throw new InvalidOperationException($"{ExeName} is missing from the package.");
            entry.ExtractToFile(newExe, overwrite: true);
        }

        var fv = FileVersionInfo.GetVersionInfo(newExe);
        var got = new Version(fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart);
        if (got != release.Version)
            throw new InvalidOperationException($"The package contains v{got}, expected {release.Tag}.");

        string old = OldPath(target);
        if (File.Exists(old)) File.Delete(old); // leftover from a run whose cleanup failed
        File.Move(target, old); // Windows allows renaming a running exe, just not overwriting it
        try
        {
            File.Move(newExe, target);
        }
        catch
        {
            File.Move(old, target);
            throw;
        }

        Log.Info($"self-update: installed {release.Tag}, restarting");
        try
        {
            Process.Start(new ProcessStartInfo(target, UpdatedArg) { UseShellExecute = false, WorkingDirectory = dir });
        }
        catch (Exception ex)
        {
            Log.Error("self-update: installed but restart failed", ex);
            throw new InvalidOperationException($"{release.Tag} is installed but could not start. Exit and open Claude Meter again.");
        }
    }

    /// <summary>
    /// --update-selftest [badsha]: install the latest release over this exe with no UI,
    /// result in the log. "badsha" forces a checksum mismatch, which must leave the exe untouched.
    /// </summary>
    public static void SelfTest(bool badSha)
    {
        try
        {
            using var http = new HttpClient();
            var release = GetLatestAsync(http).GetAwaiter().GetResult()
                ?? throw new InvalidOperationException("no release found");
            if (badSha) release = release with { Sha256 = new string('0', 64) };
            Log.Info($"update-selftest: v{Current} -> {release.Tag} (badsha={badSha})");
            InstallAsync(release, new Progress<int>()).GetAwaiter().GetResult();
            Log.Info("update-selftest: installed and restarted");
        }
        catch (Exception ex)
        {
            Log.Info($"update-selftest: failed: {ex.Message}");
        }
    }

    /// <summary>Best-effort removal of what a previous update left behind.</summary>
    public static void CleanupAfterUpdate()
    {
        try
        {
            if (Environment.ProcessPath is { } exe && File.Exists(OldPath(exe)))
            {
                File.Delete(OldPath(exe));
                Log.Info("self-update: removed previous version");
            }
        }
        catch (Exception ex) { Log.Warn($"self-update: could not remove old exe: {ex.Message}"); }
        try
        {
            if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
        }
        catch { }
    }

    static string OldPath(string exe) =>
        Path.Combine(Path.GetDirectoryName(exe)!, Path.GetFileNameWithoutExtension(exe) + ".old.exe");

    static bool IsWritable(string dir)
    {
        try
        {
            string probe = Path.Combine(dir, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, Array.Empty<byte>());
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
