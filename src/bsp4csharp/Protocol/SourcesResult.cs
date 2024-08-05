using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record SourcesResult
{
    [DataMember(Name = "items")]
    public required SourcesItem[] Items { get; init; }
}