using System.Windows;
using System.Windows.Input;
using ChatApp.Services;

namespace ChatApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        TxtUsername.Focus();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ConnectAsync();
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e) => ConnectAsync();

    private async void ConnectAsync()
    {
        TxtError.Visibility = Visibility.Collapsed;
        var host = TxtHost.Text.Trim();
        var portText = TxtPort.Text.Trim();
        var username = TxtUsername.Text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter a username.");
            return;
        }

        if (username.Length > 20)
        {
            ShowError("Username must be 20 characters or less.");
            return;
        }

        if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
        {
            ShowError("Invalid port number.");
            return;
        }

        BtnConnect.IsEnabled = false;
        BtnConnect.Content = "Connecting...";

        var service = new ChatService();
        try
        {
            await service.ConnectAsync(host, port, username);

            // Check for ERROR response (username taken, etc.)
            // We wait briefly to allow server response
            await Task.Delay(200);

            var chatWindow = new ChatWindow(service);
            chatWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            service.Disconnect();
            ShowError($"Connection failed: {ex.Message}");
            BtnConnect.IsEnabled = true;
            BtnConnect.Content = "Connect to Chat";
        }
    }

    private void ShowError(string msg)
    {
        TxtError.Text = msg;
        TxtError.Visibility = Visibility.Visible;
    }
}
