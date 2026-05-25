using RestoreWindowPlace;
using System.Windows;

namespace BorderlessWebView;

public partial class App : Application
{
    public WindowPlace WindowPlace { get; } = new WindowPlace("placement.config");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length == 0)
        {
            MessageBox.Show("Please provide a URL as a command-line argument.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var url = e.Args[0];

        var mainWindow = new WebViewerWindow(url);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        WindowPlace.Save();
    }
}
