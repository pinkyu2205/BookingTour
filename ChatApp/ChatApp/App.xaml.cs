using System.Windows;
using ChatApp.Server;

namespace ChatApp;

public partial class App : Application
{
    private ChatServer? _server;
    private CancellationTokenSource? _serverCts;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Start embedded server in background
        _serverCts = new CancellationTokenSource();
        _server = new ChatServer(port: 9000);
        _ = Task.Run(() => _server.StartAsync(_serverCts.Token));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serverCts?.Cancel();
        _server?.Stop();
        base.OnExit(e);
    }
}

