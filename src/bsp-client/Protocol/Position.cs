using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record Position
{
    /** Line position in a document (zero-based). */
    [DataMember(Name = "line")]
    public int Line { get; set; }

    /** Character offset on a line in a document (zero-based)
     * 
     * If the character value is greater than the line length it defaults back
     * to the line length. */
    [DataMember(Name = "character")]
    public int Character { get; set; }
}

