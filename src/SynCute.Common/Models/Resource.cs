using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SynCute.Common.Models;

public class Resource
{
    
    [Newtonsoft.Json.JsonIgnore]
    public string? ResourceName { get; init; }
    
    [Newtonsoft.Json.JsonIgnore]
    public string FullPath { get; init; } = null!;

    [JsonProperty("Path")]
    public string RelativePath { get; init; } = null!;


    [JsonProperty("Checksum")]
    public string? Checksum { get; init; }
}