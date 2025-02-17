using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record TestFinishData
{
    [DataMember(Name = "id")]
    public required string Id { get; set; }

    [DataMember(Name = "buildTarget")]
    public required BuildTargetIdentifier BuildTarget { get; set; }

    [DataMember(Name = "fullyQualifiedName")]
    public required string FullyQualifiedName { get; set; }

    /** Name or description of the test. */
    [DataMember(Name = "displayName")]
    public required string DisplayName { get; set; }

    /** Information about completion of the test, for example an error message. */
    [DataMember(Name = "message")]
    public string? Message { get; set; }

    /** Completion status of the test. */
    [DataMember(Name = "status")]
    public TestStatus Status { get; set; }

    /** Source location of the test, as LSP location. */
    [DataMember(Name = "location")]
    public Location? Location { get; set; }

    /** Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified. */
    [DataMember(Name = "dataKind")]
    public string? DataKind { get; set; }

    /** Optionally, structured metadata about the test completion.
    * For example: stack traces, expected/actual values. */
    [DataMember(Name = "data")]
    public TestFinishData? Data { get; set; }
}