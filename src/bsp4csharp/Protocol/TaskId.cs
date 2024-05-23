using System.Runtime.Serialization;
using Newtonsoft.Json;
using Identifier = string;

namespace bsp4csharp.Protocol;

[DataContract]
public class TaskId
{
    /** A unique identifier */
    [DataMember(Name = "id")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public required Identifier Id { get; set; }

    /** The parent task ids, if any. A non-empty parents field means
    * this task is a sub-task of every parent task id. The child-parent
    * relationship of tasks makes it possible to render tasks in
    * a tree-like user interface or inspect what caused a certain task
    * execution.
    * OriginId should not be included in the parents field, there is a separate
    * field for that. */
    [DataMember(Name = "parents")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Identifier[]? Parents { get; set; }
}