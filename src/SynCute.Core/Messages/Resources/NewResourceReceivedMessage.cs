namespace SynCute.Core.Messages.Resources;

public class NewResourceReceivedMessage : Message
{
    public const string TypeName = "new_resource";
    
    public NewResourceReceivedContent Content { get; }
    
    public NewResourceReceivedMessage(NewResourceReceivedContent content) : base(TypeName)
    {
        Content = content;
    }
}