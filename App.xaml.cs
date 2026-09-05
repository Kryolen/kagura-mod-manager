using System.Threading;
using System.Windows;

namespace KaguraModManager;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "KaguraModManager_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Kagura Mod Manager is already running.", "Already Running",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
