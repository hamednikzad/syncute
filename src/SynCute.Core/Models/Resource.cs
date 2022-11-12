namespace SynCute.Core.Models;

public class Resource
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Hash { get; set; }
    public string Type { get; set; }
    public List<Resource> Children { get; set; }
}