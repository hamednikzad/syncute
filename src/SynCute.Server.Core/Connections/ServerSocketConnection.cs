using System.Net.WebSockets;
using System.Text;
using Serilog;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Server.Core.Messages;

namespace SynCute.Server.Core.Connections;

public class ServerSocketConnection
{
    public event Action<Guid, string>? NewResourceReceived;
    public event Action<Guid>? ConnectionClosed;

    public Guid Id { get; }

    private readonly IResourceHelper _resourceHelper;
    private readonly WebSocket _socket;
    private readonly ServerMessageProcessor _messageProcessor;
    private readonly CancellationToken _cancellationToken;
    private bool _isOpen;

    public ServerSocketConnection(IResourceHelper resourceHelper, WebSocket socket, CancellationToken cancellationToken)
    {
        Id = Guid.NewGuid();
        
        _resourceHelper = resourceHelper;
        _socket = socket;
        _cancellationToken = cancellationToken;

        Log.Information("Connection with Id {Id} established", Id);
        _messageProcessor = new ServerMessageProcessor(_resourceHelper, Send, Send);
    }

    public async Task Start()
    {
        _isOpen = true;

        var listeningTask = Task.Run(async () =>
        {
            try
            {
                while (_isOpen && _socket.State == WebSocketState.Open)
                {
                    Log.Information("Listening for {Id}, Thread {Thread}", Id, Environment.CurrentManagedThreadId);
                    await WaitForReceive();
                }
            }
            catch (Exception)
            {
                ProcessCloseMessage();
            }
        }, _cancellationToken);
        
        await Send(MessageFactory.CreateReadyJsonMessage());
        await listeningTask;
    }

    #region Process
    
    private void ProcessCloseMessage()
    {
        _isOpen = false;
        Log.Information("Connection {Id} closed", Id);
        ConnectionClosed?.Invoke(Id);
    }

    private async Task ProcessBinaryMessage(MemoryStream ms)
    {
        var newResource = await _resourceHelper.WriteResource(ms);
        
        NewResourceReceived?.Invoke(Id, newResource.RelativePath);
    }

    private async Task ProcessTextMessage(Stream ms)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        Log.Information("Receive message from {Id}:{Msg}", Id, text);

        var message = MessageDeserializer.Deserialize(text);

        await _messageProcessor.Process(message);
    }

    #endregion

    #region Send/Receive

    private async Task Send(string message)
    {
        await _socket.SendAsync(ArrayHelper.GetByteArray(message), WebSocketMessageType.Text,
            true, _cancellationToken);
    }

    private async Task Send(ReadOnlyMemory<byte> message, bool endOfMessage)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary,
            endOfMessage, _cancellationToken);
    }
    
    private async Task WaitForReceive()
    {
        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
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

    #endregion

    public async Task SendNewResourceReceived(string resource)
    {
        await Send(MessageFactory.CreateNewResourceReceived(resource));
    }
}