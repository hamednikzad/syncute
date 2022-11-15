namespace SynCute.Core.Messages.Resources;

public class DownloadResourcesContent : MessageContent
{
    public string[] Files { get; }
    
    public DownloadResourcesContent(string[] files)
    {
        Files = files;
    }
}