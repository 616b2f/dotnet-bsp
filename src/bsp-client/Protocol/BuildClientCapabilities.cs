using System.Collections.Generic;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class BuildClientCapabilities
{
    /// <summary>
    /// The languages that this client supports.
    /// The ID strings for each language is defined in the LSP.
    /// The server must never respond with build targets for other
    /// languages than those that appear in this list.
    /// </summary>
    [DataMember(Name = "languageIds")]
    public ICollection<string> LanguageIds { get; init; } = new List<string>();
}