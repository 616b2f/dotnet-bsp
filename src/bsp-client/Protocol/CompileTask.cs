using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record CompileTask
{
    [DataMember(Name = "target")]
    public required BuildTargetIdentifier Target { get; set; }
}