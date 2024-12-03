using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record TestTaskData
{
    /** The build target that was compiled. */
    [DataMember(Name = "target")]
    public required BuildTargetIdentifier Target { get; set; }
}
