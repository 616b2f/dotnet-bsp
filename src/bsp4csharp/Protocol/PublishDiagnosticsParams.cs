using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class PublishDiagnosticsParams
{
    /// <summary>
    /// The document where the diagnostics are published.
    /// </summary>
    [DataMember(Name = "textDocument")]
    public required TextDocumentIdentifier TextDocument { get; set; }

    /** The build target where the diagnostics origin.
    * It is valid for one text document to belong to multiple
    * build targets, for example sources that are compiled against multiple
    * platforms (JVM, JavaScript). */
    [DataMember(Name = "buildTarget")]
    public required BuildTargetIdentifier BuildTarget { get; set; }

    /** The request id that originated this notification. */
    [DataMember(Name = "originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string? OriginId { get; set; }

    /** The diagnostics to be published by the client. */
    [DataMember(Name = "diagnostics")]
    public ICollection<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

    /** Whether the client should clear the previous diagnostics
    * mapped to the same `textDocument` and `buildTarget`. */
    [DataMember(Name = "reset")]
    public bool Reset { get; set; }
}
