using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class RunResult
{
    /** An optional request id to know the origin of this report. */
    [DataMember(Name = "originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? OriginId { get; set; }

    /** A status code for the execution. */
    [DataMember(Name = "statusCode")]
    public StatusCode StatusCode { get; set; }
}