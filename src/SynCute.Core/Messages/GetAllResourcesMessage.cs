namespace SynCute.Core.Messages;

public class GetAllResourcesMessage : Message
{
    public const string CommandName = "GetAllResourcesMessage";
    
    public GetAllResourcesMessage() : base(CommandName)
    {
    }
    
    public GetAllResourcesMessage(Message message) : base(CommandName)
    {
        Content = message.Content;
        Meta = message.Meta;
    }
    
    
}