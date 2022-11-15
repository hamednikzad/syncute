using System.Text.Json.Serialization;

namespace SynCute.Core.Models;

public class Resource
{
    
    [Newtonsoft.Json.JsonIgnore]
    public string? ResourceName { get; init; }
    
    [Newtonsoft.Json.JsonIgnore]
    public string FullPath { get; init; } = null!;

    [JsonPropertyName("Path")]
    public string RelativePath { get; init; } = null!;


    [JsonPropertyName("Checksum")]
    public string? Checksum { get; init; }
}