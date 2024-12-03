using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

[DataContract]
public record TestReportData
{
    /** The build target that was compiled. */
    [DataMember(Name = "target")]
    public required BuildTargetIdentifier Target { get; set; }

    /** The total number of milliseconds tests take to run (e.g. doesn't include compile times). */
    [DataMember(Name = "time")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? Time { get; set; }

    /** The total number of successful tests. */
    [DataMember(Name = "passed")]
    public int Passed { get; set; }

    /** The total number of failed tests. */
    [DataMember(Name = "failed")]
    public int Failed { get; set; }

    /** The total number of cancelled tests. */
    [DataMember(Name = "cancelled")]
    public int Cancelled { get; set; }

    /** The total number of ignored tests. */
    [DataMember(Name = "ignored")]
    public int Ignored { get; set; }

    /** The total number of skipped tests. */
    [DataMember(Name = "skipped")]
    public int Skipped { get; set; }
}