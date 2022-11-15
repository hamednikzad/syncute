namespace SynCute.Core.Messages.Resources;

public class NewResourceReceivedContent : MessageContent
{
    public string Resource { get; }
    
    public NewResourceReceivedContent(string resource)
    {
        Resource = resource;
    }
}