using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public class DebugSessionAddress
{
    /// <summary>
    /// The Debug Adapter Protocol server's connection uri
    /// </summary>
    [DataMember(Name = "uri")]
    public required Uri Uri { get; set; }
}