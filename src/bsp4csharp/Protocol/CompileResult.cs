using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class CompileResult
{
    /** An optional request id to know the origin of this report. */
    [DataMember(Name="originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string? OriginId { get; set; }

    /** A status code for the execution. */
    [DataMember(Name="statusCode")]
    public StatusCode StatusCode { get; set; }

    /** Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified. */
    [DataMember(Name="dataKind")]
    public string? DataKind { get; set; }

    /** A field containing language-specific information, like products
    * of compilation or compiler-specific metadata the client needs to know. */
    [DataMember(Name="data")]
    public object? Data { get; set; }
}