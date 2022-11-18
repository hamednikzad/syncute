using Serilog;
using SynCute.Common;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;

namespace SynCute.Server.Core.Messages;

public class ServerMessageProcessor : MessageProcessor
{
    private readonly Func<string, Task> _send;
    private readonly IResourceHelper _resourceHelper;

    public ServerMessageProcessor(IResourceHelper resourceHelper, Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray) : base(
        send, sendByteArray)
    {
        _resourceHelper = resourceHelper;
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
            case AllResourcesListMessage:
                await OnAllResourcesListMessage();
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
        var resources = _resourceHelper.GetResourcesWithRelativePath(message.Content.Resources);
        await UploadResources(resources);
    }

    private async Task OnPingMessage()
    {
        Log.Information("receive ping message");

        await _send(MessageFactory.CreatePongJsonMessage());
    }

    private void OnPongMessage()
    {
        Log.Information("receive pong message");
    }

    private Task OnAllResourcesListMessage()
    {
        Log.Information("AllResourcesListMessage received");
        return Task.CompletedTask;
    }

    private async Task OnGetAllResourcesMessage()
    {
        var resources = _resourceHelper.GetAllFilesWithChecksum();
        var message = MessageFactory.CreateAllResourcesListJsonMessage(resources);
        await _send(message);
    }
}