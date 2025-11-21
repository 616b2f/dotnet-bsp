using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class ReadParams
{
    // Id of the request
    [DataMember(Name="originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string? OriginId { get; set; }

    [DataMember(Name = "task")]
    public TaskId? TaskId { get; set; }

    [DataMember(Name = "message")]
    public required string Message { get; set; }
}