using Serilog;
using SynCute.Core.Helpers;

namespace SynCute.Core.Messages;

public static class MessageDeserializer
{
    public static Message? Deserialize(string data)
    {
        try
        {
            var message = JsonHelper.Deserialize<Message>(data);
            if (message is null) return null;
            
            switch (message.Command)
            {
                case GetAllResourcesMessage.CommandName:
                    return new GetAllResourcesMessage(message);
            }
            return message;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in deserializing message");
            return new UnknownMessage();
        }
    }
}