using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatApp.Models;

namespace ChatApp.Services;

public class ChatService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource _cts = new();

    public string Username { get; private set; } = string.Empty;
    public bool IsConnected => _client?.Connected ?? false;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<Exception>? Disconnected;

    public async Task ConnectAsync(string host, int port, string username)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
        Username = username;

        // Send JOIN
        await SendMessageAsync(new ChatMessage { Type = "JOIN", Username = username });

        // Start listening
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenAsync(_cts.Token));
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _reader != null)
            {
                var line = await _reader.ReadLineAsync(ct);
                if (line == null) break;

                try
                {
                    var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                    if (msg != null)
                        MessageReceived?.Invoke(msg);
                }
                catch {}
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Disconnected?.Invoke(ex);
        }
    }

    public async Task SendChatMessageAsync(string content)
    {
        var msg = new ChatMessage { Type = "MSG", Username = Username, Content = content };
        await SendMessageAsync(msg);
    }

    private async Task SendMessageAsync(ChatMessage message)
    {
        if (_stream == null) return;
        var json = JsonSerializer.Serialize(message) + "\n";
        var data = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync();
        try
        {
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        finally { _sendLock.Release(); }
    }

    public void Disconnect()
    {
        _cts.Cancel();
        _client?.Close();
    }
}
