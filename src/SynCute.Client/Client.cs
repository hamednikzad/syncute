using System.Net.WebSockets;
using System.Text;
using Serilog;
using SynCute.Core.Messages;

namespace SynCute.Client;

public class Client : IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly ClientWebSocket _socket;
    private readonly ClientMessageProcessor _messageProcessor;
    private bool _isOpen;

    public Client(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _socket = new ClientWebSocket();
        _messageProcessor = new ClientMessageProcessor(Send, Send);
    }

    private async Task Send(string message)
    {
        await _socket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text,
            true, _cancellationToken);
    }

    private async Task Send(ReadOnlyMemory<byte> message, bool endOfMessage)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary,
            endOfMessage, _cancellationToken);
    }

    private async Task WaitForReceive(CancellationToken ct)
    {
        Log.Information("Waiting for new message...");

        var buffer = new ArraySegment<byte>(new Byte[8192]);

        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, _cancellationToken);
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        switch (result.MessageType)
        {
            case WebSocketMessageType.Text:
                await ProcessTextMessage(ms);
                break;
            case WebSocketMessageType.Binary:
                break;
            case WebSocketMessageType.Close:
                _isOpen = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task Connect()
    {
        while (true)
        {
            try
            {
                Log.Information("Connecting...");
                await _socket.ConnectAsync(new Uri("ws://localhost:5206/ws"), CancellationToken.None);
                _isOpen = true;
                Log.Information("Successfully connected");
                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error in connection to server");
            }

            const int sleep = 5000;
            Log.Information("Try to connect in {Sleep}ms", sleep);
            Thread.Sleep(sleep);
        }
    }

    public async Task Start()
    {
        await Connect();

        await SendGetAllResources();

        while (!_cancellationToken.IsCancellationRequested)
        {
            await WaitForReceive(_cancellationToken);
        }
    }

    private async Task Sync()
    {
        //Get current files

        //Compare

        //Sync
    }

    private async Task SendGetAllResources()
    {
        Log.Information("Send GetAllResources message");
        var message = MessageFactory.CreateGetAllResourcesJsonMessage();
        await Send(message);
    }

    public void Dispose()
    {
        _isOpen = false;
        _socket.Dispose();
    }

    private async Task ProcessTextMessage(Stream ms)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        Log.Information("Receive message from server:{Msg}", text);

        var message = MessageDeserializer.Deserialize(text);

        await _messageProcessor.Process(message);
    }

    public async Task Stop()
    {
        if (!_isOpen)
            return;

        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing...", _cancellationToken);
        _isOpen = false;
    }
}