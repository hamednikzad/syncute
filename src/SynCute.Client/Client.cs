using System.Net.WebSockets;
using System.Text;

namespace SynCute.Client;

public class Client
{
    public async Task Start()
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("ws://localhost:6666/ws"), CancellationToken.None);
        var buffer = new byte[256];
        while (ws.State == WebSocketState.Open)
        {
            Console.WriteLine("Waiting...");
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
            else
            {
                Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, result.Count));
            }
        }
    }
}