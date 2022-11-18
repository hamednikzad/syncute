using SynCute.Common.Models;

namespace SynCute.Common.Messages.Resources;

public class AllResourcesListContent : MessageContent
{
    //0 File Path
    //1 Checksum
    public List<Resource> Resources { get; set; }
    
    public AllResourcesListContent(List<Resource> resources)
    {
        Resources = resources;
    }
}