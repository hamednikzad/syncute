using SynCute.Core.Helpers;
using SynCute.Core.Messages.Behavioral;
using SynCute.Core.Messages.Resources;
using SynCute.Core.Models;

namespace SynCute.Core.Messages;

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
}