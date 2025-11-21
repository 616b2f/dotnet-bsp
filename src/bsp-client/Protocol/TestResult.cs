using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class TestResult
{
    /** An optional request id to know the origin of this report. */
    [DataMember(Name = "originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? OriginId { get; set; }

    /** A status code for the execution. */
    [DataMember(Name = "statusCode")]
    public StatusCode StatusCode { get; set; }

    /** Kind of data to expect in the `data` field.
        * If this field is not set, the kind of data is not specified. */
    [DataMember(Name = "dataKind")]
    public string? DataKind { get; set; }

    /** Language-specific metadata about the test result.
        * See ScalaTestParams as an example. */
    [DataMember(Name = "data")]
    public object? Data { get; set; }
}