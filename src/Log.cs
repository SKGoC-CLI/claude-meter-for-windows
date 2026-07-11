namespace ClaudeMeter;

/// <summary>
/// Tiny thread-safe daily-file logger for user-shareable diagnostics.
/// Never throws, never logs tokens or other secrets.
/// </summary>
static class Log
{
    static readonly object Lock = new();

    public static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeMeter", "logs");

    public static void Info(string message) => Write("INFO ", message);
    public static void Warn(string message) => Write("WARN ", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} :: {ex}");

    static void Write(string level, string message)
    {
        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(
                    Path.Combine(Dir, $"claude-meter-{DateTime.Now:yyyy-MM-dd}.log"),
                    $"{DateTime.Now:HH:mm:ss} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // logging must never break the app
        }
    }

    public static void CleanupOldLogs(int keepDays = 7)
    {
        try
        {
            if (!Directory.Exists(Dir)) return;
            foreach (var file in Directory.GetFiles(Dir, "claude-meter-*.log"))
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-keepDays))
                    File.Delete(file);
        }
        catch
        {
        }
    }
}
