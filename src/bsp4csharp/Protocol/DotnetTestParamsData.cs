using System.Runtime.Serialization;

namespace dotnet_bsp.Handlers
{
    [DataContract]
    public class DotnetTestParamsData
    {
        [DataMember(Name = "filters")]
        public required string[] Filters { get; set; }
    }
}
