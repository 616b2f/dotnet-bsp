using System.Runtime.Serialization;

namespace BaseProtocol.Protocol
{
    [DataContract]
    public class ClientInfo
    {
        /// <summary>
        /// Gets or sets the name of the client
        /// </summary>
        [DataMember(Name = "name")]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the version of the client
        /// </summary>
        [DataMember(Name = "version")]
        public string? Version
        {
            get;
            set;
        }
    }
}
