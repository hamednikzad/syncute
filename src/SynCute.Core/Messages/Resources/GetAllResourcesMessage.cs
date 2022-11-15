namespace SynCute.Core.Messages.Resources;

public class GetAllResourcesMessage : Message
{
    public const string TypeName = "get_resources";
    
    public GetAllResourcesMessage() : base(TypeName)
    {
    }
}