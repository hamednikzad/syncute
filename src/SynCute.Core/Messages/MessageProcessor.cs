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
            return message;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in deserializing message");
            return new UnknownMessage();
        }
    }
}