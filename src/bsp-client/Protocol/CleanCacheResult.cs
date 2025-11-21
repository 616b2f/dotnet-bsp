using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class CleanCacheResult
{
    /** Optional message to display to the user. */
    [DataMember(Name = "message")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string? Message { get; set; }

    /** Indicates whether the clean cache request was performed or not. */
    [DataMember(Name = "cleaned")]
    public bool Cleaned { get; set; }
}