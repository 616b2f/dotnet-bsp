using System;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record Location
{
    [DataMember(Name = "uri")]
    public required Uri Uri { get; set; }

    [DataMember(Name = "range")]
    public required Range Range { get; set; }
}