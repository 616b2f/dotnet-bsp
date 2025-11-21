using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class TestProvider
{
    [DataMember(Name="languageIds")]
    public ICollection<string> LanguageIds { get; } = new List<string>();
}

