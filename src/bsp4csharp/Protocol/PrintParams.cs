using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

[DataContract]
public class PrintParams
{
    /** An optional request id to know the origin of this report. */
    [DataMember(Name = "originId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? OriginId { get; set; }

    /** Relevant only for test tasks.
    * Allows to tell the client from which task the output is coming from.
    **/
    [DataMember(Name = "taskId")]
    public TaskId? TaskId { get; set; }

    /** Message content can contain arbitrary bytes.
    * They should be escaped as per [javascript encoding](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Grammar_and_types#using_special_characters_in_strings)
    **/
    [DataMember(Name = "message")]
    public required string Message { get; set; }
}