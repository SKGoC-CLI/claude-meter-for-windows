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

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
