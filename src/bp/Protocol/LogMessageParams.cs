using System.Runtime.Serialization;

namespace BaseProtocol.Protocol
{
    /// <summary>
    /// Class which represents parameter sent with window/logMessage requests.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/base/0.9/specification/#logMessageParams">Base Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    public class LogMessageParams
    {
        /// <summary>
        /// Gets or sets the type of message.
        /// </summary>
        [DataMember(Name = "type")]
        public MessageType MessageType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [DataMember(Name = "message")]
        public string Message
        {
            get;
            set;
        }
    }
}

