namespace SynCute.Core.Messages.Resources;

public class DownloadResourcesMessage : Message
{
    public const string TypeName = "download";
    
    public DownloadResourcesContent Content { get; }
    
    public DownloadResourcesMessage(DownloadResourcesContent content) : base(TypeName)
    {
        Content = content;
    }
}