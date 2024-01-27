using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record BuildTargetIdentifier
{
    [DataMember(Name="uri")]
    public required Uri Uri { get; set; }
}
