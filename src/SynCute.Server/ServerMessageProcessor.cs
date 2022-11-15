using Serilog;
using SynCute.Core.Helpers;
using SynCute.Core.Messages;
using SynCute.Core.Messages.Behavioral;
using SynCute.Core.Messages.Resources;

namespace SynCute.Server;

public class ServerMessageProcessor
{
    private readonly Func<string, Task> _send;
    public event Action<Guid>? ConnectionClosed;
    public event Action? UnknownMessageReceived;

    public ServerMessageProcessor(Func<string, Task> send)
    {
        _send = send;
    }

    public async Task Process(Message message)
    {
        switch (message)
        {
            case BadMessage badMessage:
                await _send(MessageFactory.CreateBadJsonMessage(badMessage.Message));
                break;
            case PingMessage:
                await OnPingMessage();
                break;
            case PongMessage:
                OnPongMessage();
                break;
            case GetAllResourcesMessage:
                await OnGetAllResourcesMessage();
                break;
            case AllResourcesListMessage allResourcesListMessage:
                await OnAllResourcesListMessage(allResourcesListMessage);
                break;
            default:
                throw new Exception("Unknown message");
        }
    }

    private async Task OnPingMessage()
    {
        Log.Information("receive ping message");
        await _send(MessageFactory.CreatePongJsonMessage());
        Thread.Sleep(1000);
        
        await _send(MessageFactory.CreatePongJsonMessage());
    }

    private void OnPongMessage()
    {
        Log.Information("receive pong message");
    }

    private async Task OnAllResourcesListMessage(AllResourcesListMessage message)
    {
        Log.Information("AllResourcesListMessage received:");
    }

    private async Task OnGetAllResourcesMessage()
    {
        var resources = ResourceHelper.GetAllFilesWithChecksum();
        var message = MessageFactory.CreateAllResourcesListJsonMessage(resources);
        await _send(message);
    }
}