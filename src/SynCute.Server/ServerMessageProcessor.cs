using Serilog;
using SynCute.Core;
using SynCute.Core.Helpers;
using SynCute.Core.Messages;
using SynCute.Core.Messages.Behavioral;
using SynCute.Core.Messages.Resources;

namespace SynCute.Server;

public class ServerMessageProcessor : MessageProcessor
{
    private readonly Func<string, Task> _send;
    public event Action<Guid>? ConnectionClosed;
    public event Action? UnknownMessageReceived;

    public ServerMessageProcessor(Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray) : base(
        send, sendByteArray)
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
            case DownloadResourcesMessage downloadResourcesMessage:
                await OnDownloadResourcesMessage(downloadResourcesMessage);
                break;
            default:
                throw new Exception("Unknown message");
        }
    }

    private async Task OnDownloadResourcesMessage(DownloadResourcesMessage message)
    {
        var resources = ResourceHelper.GetResourcesWithRelativePath(message.Content.Files);
        await UploadResources(resources);
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