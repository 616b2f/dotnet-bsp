using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

/// <summary>
/// Class which represents the result returned by the initialize request.
///
/// See the <see href="https://build-server-protocol.github.io/docs/specification#initializebuildresult">Build Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
public class InitializeBuildResult
{
    /// <summary>
    /// Name of the server
    /// </summary>
    [DataMember(Name = "displayName")]
    public required string DisplayName { get; set; }

    /// <summary>
    /// The version of the server
    /// </summary>
    [DataMember(Name = "version")]
    public required string Version { get; set; }

    /// <summary>
    /// The BSP version that the server speaks
    /// </summary>
    [DataMember(Name = "bspVersion")]
    public required string BspVersion { get; set; }

    /// <summary>
    /// Gets or sets the server capabilities.
    /// </summary>
    [DataMember(Name = "capabilities")]
    public required BuildServerCapabilities Capabilities
    {
        get;
        set;
    }

    /// <summary>
    /// Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified.
    /// </summary>
    [DataMember(Name = "dataKind")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DataKind { get; set; }

    /// <summary>
    /// Additional metadata about the server
    /// </summary>
    [DataMember(Name = "data")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }
}
