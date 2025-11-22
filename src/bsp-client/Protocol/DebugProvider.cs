using System.Collections.Generic;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class DebugProvider
{
    [DataMember(Name="languageIds")]
    public ICollection<string> LanguageIds { get; } = new List<string>();
}