using System.Runtime.Serialization;
using Newtonsoft.Json;
using Identifier = string;

namespace bsp4csharp.Protocol
{
    [DataContract]
    public record TaskProgressParams
    {
        /** Unique id of the task with optional reference to parent task id */
        [DataMember(Name = "taskId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public required TaskId TaskId { get; set; }

        /** A unique identifier generated by the client to identify this request. */
        [DataMember(Name = "originId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Identifier? OriginId { get; set; }

        /** Timestamp of when the event started in milliseconds since Epoch. */
        [DataMember(Name = "eventTime")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? EventTime { get; set; }

        /** Message describing the task. */
        [DataMember(Name = "message")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Message { get; set; }

        /** If known, total amount of work units in this task. */
        [DataMember(Name = "total")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? Total { get; set; }

        /** If known, completed amount of work units in this task. */
        [DataMember(Name = "progress")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? Progress { get; set; }

        /** Name of a work unit. For example, "files" or "tests". May be empty. */
        [DataMember(Name = "unit")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Unit { get; set; }

        /// <summary>
        /// Kind of data to expect in the `data` field. If this field is not set, the kind of data is not specified.
        /// </summary>
        [DataMember(Name = "dataKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? DataKind { get; set; }

        /** Optional metadata about the task.
        * Objects for specific tasks like compile, test, etc are specified in the protocol. */
        [DataMember(Name = "data")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? Data { get; set; }
    }
}