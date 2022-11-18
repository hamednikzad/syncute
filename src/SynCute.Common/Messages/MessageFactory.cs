using SynCute.Common.Helpers;
using SynCute.Common.Messages.Behavioral;
using SynCute.Common.Messages.Resources;
using SynCute.Common.Models;

namespace SynCute.Common.Messages;

public static class MessageFactory
{
    private const JsonHelper.CaseType CaseType = JsonHelper.CaseType.PascalCase;

    private static GetAllResourcesMessage CreateGetAllResourcesMessage()
    {
        return new GetAllResourcesMessage();
    }

    public static string CreateGetAllResourcesJsonMessage()
    {
        var message = CreateGetAllResourcesMessage();
        return JsonHelper.Serialize(message, CaseType);
    }

    private static AllResourcesListMessage CreateAllResourcesListMessage(List<Resource> resources)
    {
        return new AllResourcesListMessage(new AllResourcesListContent(resources));
    }

    public static string CreateAllResourcesListJsonMessage(List<Resource> resources)
    {
        var message = CreateAllResourcesListMessage(resources);
        return JsonHelper.Serialize(message, CaseType);
    }

    public static string CreatePingJsonMessage()
    {
        return JsonHelper.Serialize(new PingMessage(), CaseType);
    }

    public static string CreatePongJsonMessage()
    {
        return JsonHelper.Serialize(new PongMessage(), CaseType);
    }

    public static string CreateBadJsonMessage(string message)
    {
        return JsonHelper.Serialize(new BadMessage(message), CaseType);
    }

    public static string CreateDownloadResourcesJsonMessage(string[] resources)
    {
        return JsonHelper.Serialize(new DownloadResourcesMessage(new DownloadResourcesContent(resources)), CaseType);
    }

    public static string CreateReadyJsonMessage()
    {
        return JsonHelper.Serialize(new ReadyMessage(), CaseType);
    }

    public static string CreateNewResourceReceived(string resource)
    {
        return JsonHelper.Serialize(new NewResourceReceivedMessage(new NewResourceReceivedContent(resource)), CaseType);
    }
}