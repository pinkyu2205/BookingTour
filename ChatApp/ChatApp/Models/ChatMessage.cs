namespace ChatApp.Models;

public class ChatMessage
{
    public string Type { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public List<string> Users { get; set; } = new();

    public static ChatMessage Join(string username) =>
        new() { Type = "JOIN", Username = username, Content = $"{username} has joined the chat." };

    public static ChatMessage Leave(string username) =>
        new() { Type = "LEAVE", Username = username, Content = $"{username} has left the chat." };

    public static ChatMessage Msg(string username, string content) =>
        new() { Type = "MSG", Username = username, Content = content };

    public static ChatMessage UserList(List<string> users) =>
        new() { Type = "USERLIST", Users = users };

    public static ChatMessage Error(string content) =>
        new() { Type = "ERROR", Content = content };
}
