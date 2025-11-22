using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public record BuildTarget
{
    /// <summary>
    /// The target’s unique identifier
    /// </summary>
    [DataMember(Name = "id")]
    public required BuildTargetIdentifier Id { get; set; }

    /// <summary>
    /// A human readable name for this target.
    /// May be presented in the user interface.
    /// Should be unique if possible.
    /// The id.uri is used if None.
    /// </summary>
    [DataMember(Name = "displayName")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The directory where this target belongs to. Multiple build targets are allowed to map
    /// to the same base directory, and a build target is not required to have a base directory.
    /// A base directory does not determine the sources of a target, see buildTarget/sources.
    /// </summary>
    [DataMember(Name = "baseDirectory")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Uri? BaseDirectory { get; set; }

    /// <summary>
    /// Free-form string tags to categorize or label this build target.
    /// For example, can be used by the client to:
    /// - customize how the target should be translated into the client's project model.
    /// - group together different but related targets in the user interface.
    /// - display icons or colors in the user interface.
    /// Pre-defined tags are listed in `BuildTargetTag` but clients and servers
    /// are free to define new tags for custom purposes.
    /// </summary>
    [DataMember(Name = "tags")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IReadOnlyCollection<BuildTargetTag> Tags { get; set; } = [];

    /// <summary>
    /// The set of languages that this target contains.
    /// The ID string for each language is defined in the LSP.
    /// </summary>
    [DataMember(Name = "languageIds")]
    public IReadOnlyCollection<string> LanguageIds { get; set; } = [];

    /// <summary>
    /// The direct upstream build target dependencies of this build target
    /// </summary>
    [DataMember(Name = "dependencies")]
    public IReadOnlyCollection<BuildTargetIdentifier> Dependencies { get; set; } = [];

    /// <summary>
    /// The capabilities of this build target.
    /// </summary>
    [DataMember(Name = "capabilities")]
    public required BuildTargetCapabilities Capabilities { get; set; }

    /// <summary>
    /// Kind of data to expect in the `data` field. If this field is not set,
    /// the kind of data is not specified.
    /// </summary>
    [DataMember(Name = "dataKind")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public BuildTargetDataKind? DataKind { get; set; }

    /// <summary>
    /// Language-specific metadata about this target.
    /// See ScalaBuildTarget as an example.
    /// </summary>
    [DataMember(Name = "data")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }
}