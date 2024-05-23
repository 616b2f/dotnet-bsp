using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class WorkspaceBuildTargetsResult
{
    /// <summary>
    /// The build targets in this workspace that
    /// contain sources with the given language ids.
    /// </summary>
    [DataMember(Name="targets")]
    public IReadOnlyCollection<BuildTarget> Targets { get; set; } = [];
}