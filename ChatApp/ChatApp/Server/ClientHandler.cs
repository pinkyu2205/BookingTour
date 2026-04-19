using System.IO;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ChatApp.Models;

namespace ChatApp.Server;

public class ClientHandler
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly ConcurrentDictionary<string, ClientHandler> _clients;
    private readonly ChatServer _server;
    private string _username = string.Empty;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public ClientHandler(TcpClient tcpClient, ConcurrentDictionary<string, ClientHandler> clients, ChatServer server)
    {
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
        _clients = clients;
        _server = server;
    }

    public async Task HandleAsync()
    {
        var remoteEndpoint = _tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[+] New connection from {remoteEndpoint}");

        try
        {
            using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);

            // First message must be a JOIN
            var firstLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(firstLine)) return;

            ChatMessage? firstMsg;
            try { firstMsg = JsonSerializer.Deserialize<ChatMessage>(firstLine); }
            catch { return; }

            if (firstMsg?.Type != "JOIN" || string.IsNullOrWhiteSpace(firstMsg.Username))
            {
                await SendAsync(ChatMessage.Error("First message must be a JOIN with a valid username."));
                return;
            }

            _username = firstMsg.Username.Trim();

            if (!_server.TryAddClient(_username, this))
            {
                await SendAsync(ChatMessage.Error($"Username '{_username}' is already taken."));
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[JOIN] {_username} joined from {remoteEndpoint}");
            Console.ResetColor();

            await _server.BroadcastAsync(ChatMessage.Join(_username));
            await _server.BroadcastUserListAsync();

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                ChatMessage? msg;
                try { msg = JsonSerializer.Deserialize<ChatMessage>(line); }
                catch { continue; }

                if (msg == null) continue;

                if (msg.Type == "MSG" && !string.IsNullOrWhiteSpace(msg.Content))
                {
                    var broadcast = ChatMessage.Msg(_username, msg.Content);
                    Console.WriteLine($"[MSG] {_username}: {msg.Content}");
                    await _server.BroadcastAsync(broadcast);
                }
            }
        }
        catch (IOException) { /* client disconnected */ }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {_username}: {ex.Message}");
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    private async Task DisconnectAsync()
    {
        if (!string.IsNullOrEmpty(_username))
        {
            _server.RemoveClient(_username);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[LEAVE] {_username} disconnected.");
            Console.ResetColor();
            await _server.BroadcastAsync(ChatMessage.Leave(_username));
            await _server.BroadcastUserListAsync();
        }
        _tcpClient.Close();
    }

    public async Task SendAsync(ChatMessage message)
    {
        var json = JsonSerializer.Serialize(message) + "\n";
        var data = Encoding.UTF8.GetBytes(json);
        await SendRawAsync(data);
    }

    public async Task SendRawAsync(byte[] data)
    {
        await _sendLock.WaitAsync();
        try
        {
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        catch { /* ignore send errors */ }
        finally
        {
            _sendLock.Release();
        }
    }
}
