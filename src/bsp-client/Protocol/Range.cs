using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record Range
{
    [DataMember(Name = "start")]
    public required Position Start { get; set; }
    [DataMember(Name = "end")]
    public required Position End { get; set; }
}

