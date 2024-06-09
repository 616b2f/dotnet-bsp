using System.Runtime.Serialization;
using Newtonsoft.Json;
using Identifier = string;

namespace bsp4csharp.Protocol;

[DataContract]
public record CompileReport
{
    [DataMember(Name = "target")]
    public required BuildTargetIdentifier Target { get; set; }

    [DataMember(Name = "originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Identifier? OriginId { get; set; }

    [DataMember(Name = "errors")]
    public int Errors { get; set; }

    [DataMember(Name = "warnings")]
    public int Warnings { get; set; }

    [DataMember(Name = "time")]
    public long? Time { get; set; }

    [DataMember(Name = "noOp")]
    public bool? NoOp { get; set; }
}