using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class Diagnostic
{
    /** The range at which the message applies. */
    [DataMember(Name = "range")]
    public required Range Range { get; set; }

    /** The diagnostic's severity. Can be omitted. If omitted it is up to the
    * client to interpret diagnostics as error, warning, info or hint. */
    [DataMember(Name = "severity")]
    public DiagnosticSeverity? Severity { get; set; }

    /** The diagnostic's code, which might appear in the user interface. */
    [DataMember(Name = "code")]
    public string? Code { get; set; }

    /** An optional property to describe the error code. */
    // public CodeDescription? CodeDescription { get; set; }

    /** A human-readable string describing the source of this
    * diagnostic, e.g. 'typescript' or 'super lint'. */
    [DataMember(Name = "source")]
    public string? Source { get; set; }

    /** The diagnostic's message. */
    [DataMember(Name = "message")]
    public required string Message { get; set; }

    // /** Additional metadata about the diagnostic. */
    // public DiagnosticTag[]? Tags { get; set; }
    //
    // /** An array of related diagnostic information, e.g. when symbol-names within
    // * a scope collide all definitions can be marked via this property. */
    // public DiagnosticRelatedInformation[]? RelatedInformation { get; set; }
    //
    // /** Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified. */
    // public DiagnosticDataKind? DataKind { get; set; }
    //
    // /** A data entry field that is preserved between a
    // * `textDocument/publishDiagnostics` notification and
    // * `textDocument/codeAction` request. */
    // public DiagnosticData? Data { get; set; }
}
