namespace BaseProtocol.Protocol
{
    using System.ComponentModel;
    using Newtonsoft.Json;

    /// <summary>
    /// Value representing the language server trace setting.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#traceValue">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter<TraceSetting>))]
    [TypeConverter(typeof(StringEnumConverter<TraceSetting>.TypeConverter))]
    public readonly record struct TraceSetting(string Value) : IStringEnum
    {
        /// <summary>
        /// Setting for 'off'.
        /// </summary>
        public static readonly TraceSetting Off = new("off");

        /// <summary>
        /// Setting for 'messages'.
        /// </summary>
        public static readonly TraceSetting Messages = new("messages");

        /// <summary>
        /// Setting for 'verbose'.
        /// </summary>
        public static readonly TraceSetting Verbose = new("verbose");
    }
}
