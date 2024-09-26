using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record TestCaseDiscoveredData
{
    [DataMember(Name = "displayName")]
    public required string DisplayName { get; set; }

    [DataMember(Name = "fullyQualifiedName")]
    public required string FullyQualifiedName { get; set; }

    [DataMember(Name = "source")]
    public required string Source { get; set; }

    [DataMember(Name = "line")]
    public int Line { get; set; }

    [DataMember(Name = "filePath")]
    public required string FilePath { get; set; }

    [DataMember(Name = "buildTarget")]
    public required BuildTargetIdentifier BuildTarget { get; set; }
}