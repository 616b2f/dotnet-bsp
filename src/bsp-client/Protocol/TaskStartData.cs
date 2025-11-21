using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record TaskStartData
{
    /** Display name of the test **/
    [DataMember(Name = "displayName")]
    public required string DisplayName { get; set; }

    /** Source location of the test, as LSP location. **/
    [DataMember(Name = "location")]
    public Location? Location { get; set; }
}