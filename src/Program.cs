using System.Reflection;

namespace ClaudeMeter;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, "ClaudeMeter_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            return; // another instance is already running
        }

        // crash diagnostics for users who report issues
        Application.ThreadException += (_, e) => Log.Error("Unhandled UI exception", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error("Unhandled exception", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => { Log.Error("Unobserved task exception", e.Exception); e.SetObserved(); };

        Log.CleanupOldLogs();
        Log.Info($"=== Claude Meter v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)} starting ===");

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());

        Log.Info("=== Claude Meter exiting ===");
    }
}
