using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

/// <summary>
/// Class which represents the parameter sent with an initialize method request.
///
/// See the <see href="https://build-server-protocol.github.io/docs/specification#initializebuildparams">Build Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
public class InitializeBuildParams
{
    /// <summary>
    /// Name of the client
    /// </summary>
    [DataMember(Name = "displayName")]
    public string DisplayName { get; set; }

    /// <summary>
    /// The version of the client
    /// </summary>
    [DataMember(Name = "version")]
    public string Version { get; set; }

    /// <summary>
    /// The BSP version that the client speaks
    /// </summary>
    [DataMember(Name = "bspVersion")]
    public string BspVersion { get; set; }

    /// <summary>
    /// Gets or sets the capabilities supported by the client.
    /// </summary>
    [DataMember(Name = "capabilities")]
    public BuildClientCapabilities Capabilities
    {
        get;
        set;
    }

    /// <summary>
    /// The rootUri of the workspace
    /// </summary>
    [DataMember(Name = "rootUri")]
    public Uri RootUri { get; set; }

    /// <summary>
    /// Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified.
    /// </summary>
    [DataMember(Name = "dataKind")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DataKind { get; set; }

    /// <summary>
    /// Additional metadata about the client
    /// </summary>
    [DataMember(Name = "data")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }
}