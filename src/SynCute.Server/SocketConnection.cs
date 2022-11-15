using System.Net.WebSockets;
using System.Text;
using Serilog;
using SynCute.Core.Helpers;
using SynCute.Core.Messages;

namespace SynCute.Server;

public class SocketConnection
{
    public event Action<Guid, string>? MessageReceived;
    public event Action<Guid>? ConnectionClosed;

    public Guid Id { get; }
    private readonly WebSocket _socket;
    private readonly ServerMessageProcessor _messageProcessor;

    public SocketConnection(WebSocket socket)
    {
        Id = Guid.NewGuid();
        _socket = socket;

        Log.Information("Connection with Id {Id} established", Id);
        _messageProcessor = new ServerMessageProcessor(Send);
        _messageProcessor.ConnectionClosed += id => ConnectionClosed?.Invoke(id);
        _messageProcessor.UnknownMessageReceived += async () => await Send("UnknownMessage");
    }

    private bool _isOpen;

    public async Task Start()
    {
        _isOpen = true;
        while (_isOpen && _socket.State == WebSocketState.Open)
        {
            Log.Information("Listening for {Id}, Thread {Thread}", Id, Environment.CurrentManagedThreadId);
            await WaitForReceive();
        }
        var t1 = Task.Run(async () =>
        {
            
        });
        
        // while (_isOpen && _socket.State == WebSocketState.Open)
        // {
        //     //await Task.Delay(1000);
        //     Log.Information("Listening for {Id}, Thread {Thread}", Id, Environment.CurrentManagedThreadId);
        //     await Send(MessageFactory.CreatePingJsonMessage());
        //     Thread.Sleep(3000);
        // }

        await t1;
    }

    private async Task WaitForReceive()
    {
        var receivedSize = 0;
        var count = 0;
        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
            receivedSize += result.Count;
            count++;
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        switch (result.MessageType)
        {
            case WebSocketMessageType.Text:
                await ProcessTextMessage(ms);
                break;
            case WebSocketMessageType.Binary:
                await ProcessBinaryMessage(ms);
                break;
            case WebSocketMessageType.Close:
                ProcessCloseMessage();
                _isOpen = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessCloseMessage()
    {
        _isOpen = false;
        Log.Information("Connection {Id} closed", Id);
        ConnectionClosed?.Invoke(Id);
    }

    private async Task ProcessBinaryMessage(MemoryStream ms)
    {
        await ResourceHelper.Write(ms);
    }

    private async Task ProcessTextMessage(Stream ms)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        Log.Information("Receive message from {Id}:{Msg}", Id, text);

        var message = MessageDeserializer.Deserialize(text);

        await _messageProcessor.Process(message);

        // MessageReceived?.Invoke(Id, text);
    }

    public async Task Send(string message)
    {
        await _socket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }

    private async Task Send(byte[] message)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}