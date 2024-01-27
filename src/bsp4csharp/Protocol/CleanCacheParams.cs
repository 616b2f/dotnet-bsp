using System.Runtime.Serialization;

namespace bsp4csharp.Protocol
{
    [DataContract]
    public class CleanCacheParams
    {
        /** A sequence of build targets to clean. */
        [DataMember(Name="targets")]
        public BuildTargetIdentifier[] Targets { get; set; } = Array.Empty<BuildTargetIdentifier>();
    }
}
