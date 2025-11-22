using System;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record SourcesItem
{
    [DataMember(Name = "target")]
    public required BuildTargetIdentifier Target { get; init; }

    // <summary>
    // The text documents or and directories that belong to this build target.
    // </summary>
    [DataMember(Name = "sources")]
    public required SourceItem[] Sources { get; init; }


    // <summary>
    // The root directories from where source files should be relativized.
    // Example: ["file://Users/name/dev/metals/src/main/scala"]
    // </summary>
    [DataMember(Name = "roots")]
    public Uri[]? Roots { get; init; }
}