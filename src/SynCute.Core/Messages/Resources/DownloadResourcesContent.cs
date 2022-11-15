namespace SynCute.Core.Messages.Resources;

public class DownloadResourcesContent : MessageContent
{
    public string[] Resources { get; }
    
    public DownloadResourcesContent(string[] resources)
    {
        Resources = resources;
    }
}