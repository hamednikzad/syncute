namespace SynCute.Common.Messages.Resources;

public class AllResourcesListMessage : Message
{
    public const string TypeName = "resources";
    
    public AllResourcesListContent Content { get; }
    
    public AllResourcesListMessage(AllResourcesListContent content) : base(TypeName)
    {
        Content = content;
    }
}