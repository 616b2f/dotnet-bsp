namespace BaseProtocol.Protocol
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the parameter sent with an initialize method request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InitializeParams : IWorkDoneProgressParams
    {
        /// <summary>
        /// Gets or sets the ID of the process which launched the language server.
        /// </summary>
        [DataMember(Name = "processId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? ProcessId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the locale the client is currently showing the user interface in.
        /// This must not necessarily be the locale of the operating system.
        ///
        /// Uses IETF language tags as the value's syntax.
        /// (See https://en.wikipedia.org/wiki/IETF_language_tag)
        /// </summary>
        [DataMember(Name = "locale")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Locale
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initialization options as specified by the client.
        /// </summary>
        [DataMember(Name = "initializationOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? InitializationOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets information about the client.
        /// </summary>
        [DataMember(Name = "clientInfo")]
        public ClientInfo? ClientInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the capabilities supported by the client.
        /// </summary>
        [DataMember(Name = "capabilities")]
        public object? Capabilities
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initial trace setting.
        /// </summary>
        [DataMember(Name = "trace")]
        [DefaultValue(typeof(TraceSetting), "off")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TraceSetting Trace
        {
            get;
            set;
        } = TraceSetting.Off;

        [DataMember(Name = "workDoneToken")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string? WorkDoneToken { get; set; }
    }

    /// <summary>
    /// Class which represents the result returned by the initialize request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeResult">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InitializeResult
    {
        /// <summary>
        /// Gets or sets the server capabilities.
        /// </summary>
        [DataMember(Name = "capabilities")]
        public object? Capabilities
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the information about the server.
        /// </summary>
        [DataMember(Name = "initializationOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ServerInfo? ServerInfo
        {
            get;
            set;
        }
    }
}