using System.Net.WebSockets;
using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Server.Connections;

public class WebConnection
{
    public event Action<Guid>? ConnectionClosed;

    public Guid Id { get; }
    private readonly WebSocket _socket;
    private bool _isOpen;

    public WebConnection(WebSocket socket)
    {
        Id = Guid.NewGuid();
        _socket = socket;
        _isOpen = true;
    }

    public bool IsConnected()
    {
        return _socket.State is WebSocketState.Connecting or WebSocketState.Open;
    }
    
    public async Task Start()
    {
        try
        {
            while (_isOpen && _socket.State == WebSocketState.Open)
            {
                Log.Information("Listening for {Id}, Thread {Thread}", Id, Environment.CurrentManagedThreadId);
                await WaitForReceive();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception occured while WaitForReceive");
            ProcessCloseMessage();
        }
    }

    #region Process

    private void ProcessCloseMessage()
    {
        _isOpen = false;
        Log.Information("Connection {Id} closed", Id);
        ConnectionClosed?.Invoke(Id);
    }

    #endregion

    #region Send/Receive

    public async Task Send(string message)
    {
        await _socket.SendAsync(ArrayHelper.GetByteArray(message), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }

    private async Task WaitForReceive()
    {
        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        switch (result.MessageType)
        {
            case WebSocketMessageType.Close:
                ProcessCloseMessage();
                _isOpen = false;
                break;
            case WebSocketMessageType.Text:
            case WebSocketMessageType.Binary:
                break;
        }
    }

    #endregion
}