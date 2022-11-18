using Serilog;
using SynCute.Common;
using SynCute.Common.Helpers;
using SynCute.Common.Messages;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;
using SynCute.Common.Models;

namespace SynCute.Client.Core.Messages;

public class ClientMessageProcessor : MessageProcessor
{
    private readonly IResourceHelper _resourceHelper;
    
    public ClientMessageProcessor(IResourceHelper resourceHelper, Func<string, Task> send, Func<ReadOnlyMemory<byte>, bool, Task> sendByteArray) : base(
        send, sendByteArray)
    {
        _resourceHelper = resourceHelper;
    }

    public async Task Process(Message message)
    {
        switch (message)
        {
            case BadMessage badMessage:
                await OnBadMessage(badMessage);
                break;
            case PingMessage:
                await OnPingMessage();
                break;
            case PongMessage:
                OnPongMessage();
                break;
            case ReadyMessage:
                await OnReadyMessage();
                break;
            case AllResourcesListMessage allResourcesListMessage:
                await OnAllResourcesListMessage(allResourcesListMessage);
                break;
            case NewResourceReceivedMessage newResourceReceivedMessage:
                await OnNewResourceReceivedMessage(newResourceReceivedMessage);
                break;
            default:
                throw new Exception("Unknown message");
        }
    }

    private async Task OnNewResourceReceivedMessage(NewResourceReceivedMessage message)
    {
        Log.Information("NewResourceReceivedMessage: {NewResource}", message.Content.Resource);
        
        await DownloadResources(new List<Resource>()
        {
            new()
            {
                RelativePath = message.Content.Resource
            }
        });
    }

    private async Task OnReadyMessage()
    {
        await SendGetAllResources();
    }

    private async Task SendGetAllResources()
    {
        Log.Information("Send GetAllResources message");
        var message = MessageFactory.CreateGetAllResourcesJsonMessage();
        await Send(message);
    }

    private async Task OnBadMessage(BadMessage badMessage)
    {
        Log.Information("BadMessaged received: {Message}", badMessage.Message);
        await Send(MessageFactory.CreateBadJsonMessage(badMessage.Message));
    }

    private async Task OnPingMessage()
    {
        Log.Information("PingMessage received");
        await Send(MessageFactory.CreatePongJsonMessage());
    }

    private void OnPongMessage()
    {
        Log.Information("PongMessage received");
    }

    private async Task OnAllResourcesListMessage(AllResourcesListMessage message)
    {
        Log.Information("AllResourcesListMessage received");
        var serverResources = message.Content.Resources;
        var serverRelativePaths = serverResources.Select(r => r.RelativePath).ToList();

        var localResources = _resourceHelper.GetAllFilesWithChecksum();
        var localRelativePaths = localResources.Select(r => r.RelativePath).ToList();

        var shouldDownloads = serverResources.ExceptBy(localRelativePaths, r => r.RelativePath).ToList();
        var shouldUploads = localResources.ExceptBy(serverRelativePaths, r => r.RelativePath).ToList();
        var intersects = serverResources.IntersectBy(localRelativePaths, r => r.RelativePath).ToList();

        foreach (var resource in intersects)
        {
            if (localResources.Any(r => r.RelativePath == resource.RelativePath && r.Checksum != resource.Checksum))
            {
                shouldDownloads.Add(resource);
            }
        }

        await UploadResources(shouldUploads);

        await DownloadResources(shouldDownloads);
    }
}