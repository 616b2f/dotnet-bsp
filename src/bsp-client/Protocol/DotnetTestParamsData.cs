using System.Runtime.Serialization;

namespace dotnet_bsp.Handlers
{
    [DataContract]
    public class DotnetTestParamsData
    {
        [DataMember(Name = "filter")]
        public required string Filter { get; set; }

        [DataMember(Name = "runSettings")]
        public string? RunSettings { get; set; }
    }
}