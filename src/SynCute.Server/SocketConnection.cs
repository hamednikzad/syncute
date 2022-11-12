using System.Net.WebSockets;
using System.Text;

public class SocketConnection
{
    public Guid Id { get; }
    private readonly WebSocket _socket;

    public SocketConnection(WebSocket socket)
    {
        Id = Guid.NewGuid();
        _socket = socket;
    }

    public async Task Handle()
    {
        while (true)
        {
            await Send();

            await Task.Delay(1000);
        }
    }

    private async Task Send()
    {
        await Send($"Test - {DateTime.Now}");
    }

    private async Task Send(string message)
    {
        await _socket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }

    private async Task Send(byte[] message)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}