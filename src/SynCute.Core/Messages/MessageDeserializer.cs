using Newtonsoft.Json.Linq;
using Serilog;
using SynCute.Core.Helpers;
using SynCute.Core.Messages.Behavioral;
using SynCute.Core.Messages.Resources;

namespace SynCute.Core.Messages;

public static class MessageDeserializer
{
    public static Message Deserialize(string data)
    {
        try
        {
            var parsedObject = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(data)!;
            var t = parsedObject["Type"];
            var type = Convert.ToString(t)?.ToLower();
            if (type is null or BadMessage.CommandName)
            {
                return new BadMessage("Type is missing");
            }
            
            
            switch (type)
            {
                case PingMessage.CommandName:
                    return new PingMessage();
                    
                case PongMessage.CommandName:
                    return new PongMessage();
                    
                case GetAllResourcesMessage.TypeName:
                    return new GetAllResourcesMessage();
                
                case AllResourcesListMessage.TypeName:
                    var content = parsedObject["Content"];
                    if (content == null)
                        return new BadMessage("Content is missing");
                    
                    var listMessage = content.ToObject<AllResourcesListContent>();
                    if (listMessage != null) return new AllResourcesListMessage(listMessage);
                    break;
                default:
                    throw new Exception("Unknown message");
            }

            return new BadMessage("Unknown message");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in deserializing message");
            return new BadMessage("Error in deserializing message");
        }
    }
}