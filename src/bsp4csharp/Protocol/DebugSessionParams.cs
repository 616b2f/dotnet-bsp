using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class DebugSessionParams
{
    /** A sequence of build targets to run. */
    [DataMember(Name="targets")]
    public required BuildTargetIdentifier[] Targets { get; set; }

    /// <summary>
    /// Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified.
    /// </summary>
    [DataMember(Name = "data")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }

    /// <summary>
    /// Language-specific metadata for this execution.
    /// See ScalaMainClass as an example.
    /// </summary>
    [DataMember(Name = "dataKind")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DataKind { get; set; }
}