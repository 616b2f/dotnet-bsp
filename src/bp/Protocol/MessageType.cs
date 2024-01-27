using System.Runtime.Serialization;

namespace BaseProtocol.Protocol
{
    /// <summary>
    /// Message type enum.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/base/0.9/specification/#messageType">Base Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    public enum MessageType
    {
        /// <summary>
        /// Error message.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Info message.
        /// </summary>
        Info = 3,

        /// <summary>
        /// Log message.
        /// </summary>
        Log = 4,

        /// <summary>
        /// Debug message.
        /// </summary>
        Debug = 5,
    }
}
