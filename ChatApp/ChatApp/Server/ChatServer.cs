using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ChatApp.Models;

namespace ChatApp.Server;

public class ChatServer
{
    private readonly int _port;
    private readonly ConcurrentDictionary<string, ClientHandler> _clients = new();
    private TcpListener? _listener;

    public int Port => _port;

    public ChatServer(int port = 9000)
    {
        _port = port;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER] Chat Server started on port {_port}");
        Console.ResetColor();
        Console.WriteLine("[SERVER] Waiting for connections...\n");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);
                var handler = new ClientHandler(tcpClient, _clients, this);
                _ = Task.Run(() => handler.HandleAsync(), ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Server accept failed: {ex.Message}");
        }
        finally
        {
            _listener.Stop();
        }
    }

    public void Stop()
    {
        _listener?.Stop();
    }

    public async Task BroadcastAsync(ChatMessage message, string? excludeUsername = null)
    {
        var json = JsonSerializer.Serialize(message) + "\n";
        var data = Encoding.UTF8.GetBytes(json);

        var tasks = _clients
            .Where(kvp => kvp.Key != excludeUsername)
            .Select(kvp => kvp.Value.SendRawAsync(data))
            .ToList();

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastUserListAsync()
    {
        var users = _clients.Keys.OrderBy(u => u).ToList();
        var msg = ChatMessage.UserList(users);
        var json = JsonSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);

        var tasks = _clients.Values.Select(c => c.SendRawAsync(data)).ToList();
        await Task.WhenAll(tasks);
    }

    public void RemoveClient(string username) => _clients.TryRemove(username, out _);

    public bool TryAddClient(string username, ClientHandler handler) =>
        _clients.TryAdd(username, handler);
}
