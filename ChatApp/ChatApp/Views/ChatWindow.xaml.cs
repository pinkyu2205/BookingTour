using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Views;

public partial class ChatWindow : Window
{
    private readonly ChatService _service;

    public ChatWindow(ChatService service)
    {
        InitializeComponent();
        _service = service;

        TxtCurrentUser.Text = _service.Username;
        Title = $"ChatApp – {_service.Username}";

        _service.MessageReceived += OnMessageReceived;
        _service.Disconnected += OnDisconnected;

        TxtMessage.Focus();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _service.Disconnect();
    }

    private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
        {
            e.Handled = true;
            SendMessage();
        }
    }

    private void BtnSend_Click(object sender, RoutedEventArgs e) => SendMessage();

    private async void SendMessage()
    {
        var text = TxtMessage.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        TxtMessage.Clear();
        try
        {
            await _service.SendChatMessageAsync(text);
        }
        catch (Exception ex)
        {
            AppendSystemMessage($"Failed to send: {ex.Message}", isError: true);
        }
    }

    private void OnMessageReceived(ChatMessage msg)
    {
        Dispatcher.Invoke(() => HandleMessage(msg));
    }

    private void OnDisconnected(Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = $"Disconnected: {ex.Message}";
            TxtStatus.Foreground = (SolidColorBrush)FindResource("RedBrush");
            BtnSend.IsEnabled = false;
            TxtMessage.IsEnabled = false;
        });
    }

    private void HandleMessage(ChatMessage msg)
    {
        switch (msg.Type)
        {
            case "MSG":
                AppendChatBubble(msg.Username, msg.Content, msg.Timestamp);
                break;
            case "JOIN":
                AppendSystemMessage($"🟢 {msg.Content}");
                break;
            case "LEAVE":
                AppendSystemMessage($"🔴 {msg.Content}");
                break;
            case "USERLIST":
                UpdateUserList(msg.Users);
                break;
            case "ERROR":
                AppendSystemMessage($"⚠️ {msg.Content}", isError: true);
                break;
        }
    }

    private void AppendChatBubble(string username, string content, string timestamp)
    {
        bool isOwn = username == _service.Username;

        // Outer container
        var row = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 4),
            HorizontalAlignment = isOwn ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };

        // Avatar placeholder (other messages)
        if (!isOwn)
        {
            var avatarBorder = new Border
            {
                Width = 36, Height = 36,
                CornerRadius = new CornerRadius(18), // Make fully round
                Background = new LinearGradientBrush(
                    Color.FromRgb(108, 99, 255), Color.FromRgb(167, 139, 250),
                    new Point(0, 0), new Point(1, 1)),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            var initial = new TextBlock
            {
                Text = username.Length > 0 ? username[0].ToString().ToUpper() : "?",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI")
            };
            avatarBorder.Child = initial;
            row.Children.Add(avatarBorder);
        }

        // Bubble
        var bubbleBg = isOwn
            ? new SolidColorBrush(Color.FromRgb(108, 99, 255)) // Muted purplish primary color for own messages
            : new SolidColorBrush(Color.FromRgb(26, 30, 46));

        var bubble = new Border
        {
            CornerRadius = isOwn
                ? new CornerRadius(14, 14, 2, 14)
                : new CornerRadius(2, 14, 14, 14),
            Background = bubbleBg,
            Padding = new Thickness(14, 10, 14, 10),
            MaxWidth = 420
        };

        var bubbleContent = new StackPanel();

        if (!isOwn)
        {
            var uname = new TextBlock
            {
                Text = username,
                FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(167, 139, 250)),
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            bubbleContent.Children.Add(uname);
        }

        var msgText = new TextBlock
        {
            Text = content,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(240, 242, 255)),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Segoe UI")
        };
        bubbleContent.Children.Add(msgText);

        var ts = string.IsNullOrEmpty(timestamp) ? DateTime.Now.ToString("HH:mm") : timestamp;
        var timeText = new TextBlock
        {
            Text = ts,
            FontSize = 10,
            Foreground = isOwn ? new SolidColorBrush(Color.FromRgb(200, 200, 255)) : new SolidColorBrush(Color.FromRgb(80, 88, 112)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0),
            FontFamily = new FontFamily("Segoe UI")
        };
        bubbleContent.Children.Add(timeText);

        bubble.Child = bubbleContent;
        row.Children.Add(bubble);

        // Fade-in animation
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        row.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        MessagesPanel.Children.Add(row);
        MessagesScroll.ScrollToEnd();
    }

    private void AppendSystemMessage(string text, bool isError = false)
    {
        var color = isError ? Color.FromRgb(231, 76, 60) : Color.FromRgb(80, 88, 112);
        var container = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 6),
            Padding = new Thickness(14, 5, 14, 5),
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B))
        };
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = new SolidColorBrush(color),
            FontFamily = new FontFamily("Segoe UI"),
            FontStyle = FontStyles.Italic
        };
        container.Child = tb;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        container.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        MessagesPanel.Children.Add(container);
        MessagesScroll.ScrollToEnd();
    }

    private void UpdateUserList(List<string> users)
    {
        UserList.ItemsSource = null;
        UserList.ItemsSource = users;
        TxtOnlineCount.Text = $"{users.Count} user{(users.Count != 1 ? "s" : "")}";
    }
}
