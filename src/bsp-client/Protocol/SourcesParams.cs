using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record SourcesParams
{

    [DataMember(Name = "targets")]
    public required BuildTargetIdentifier[] Targets { get; init; }
}