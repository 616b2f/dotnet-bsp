using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol
{
    [DataContract]
    public class BuildTargetCapabilities
    {
        /** This target can be compiled by the BSP server. */
        [DataMember(Name="canCompile")]
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public bool? CanCompile { get; set; }

        /** This target can be tested by the BSP server. */
        [DataMember(Name="canTest")]
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public bool? CanTest { get; set; }

        /** This target can be run by the BSP server. */
        [DataMember(Name="canRun")]
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public bool? CanRun { get; set; }

        /** This target can be debugged by the BSP server. */
        [DataMember(Name="canDebug")]
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public bool? CanDebug { get; set; }
    }
}
