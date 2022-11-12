﻿using System.Net.WebSockets;
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

    public SocketConnection(WebSocket socket)
    {
        Id = Guid.NewGuid();
        _socket = socket;
        
        Log.Information("Connection with Id {Id} established", Id);
    }

    private bool _isOpen;
    
    public async Task Start()
    {
        _isOpen = true;
        await Send("Hello from server!");
        while (_isOpen)
        {
            //await Task.Delay(1000);
            Log.Information("Listening for {Id}, Thread {Thread}", Id, Environment.CurrentManagedThreadId);
            await WaitForReceive();
        }
    }

    private async Task WaitForReceive()
    {
        var buffer = new ArraySegment<byte>(new Byte[8192]);

        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
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

    private async Task ProcessTextMessage(Stream ms)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        Log.Information("Receive message from {Id}:{Msg}", Id, text);
        
        var message = MessageProcessor.Process(text);
        
        if(message is null)
            return;
        
        
        MessageReceived?.Invoke(Id, text);
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
