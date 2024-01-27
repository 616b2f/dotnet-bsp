using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace bsp4csharp.Protocol
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BuildTargetTag
    {
        /** Target contains source code for producing any kind of application, may have
        * but does not require the `canRun` capability. */
        [EnumMember(Value="application")]
        Application,

        /** Target contains source code to measure performance of a program, may have
        * but does not require the `canRun` build target capability. */
        [EnumMember(Value="benchmark")]
        Benchmark,

        /** Target contains source code for integration testing purposes, may have
        * but does not require the `canTest` capability.
        * The difference between "test" and "integration-test" is that
        * integration tests traditionally run slower compared to normal tests
        * and require more computing resources to execute. */
        [EnumMember(Value="integration-test")]
        IntegrationTest,

        /** Target contains re-usable functionality for downstream targets. May have any
        * combination of capabilities. */
        [EnumMember(Value="library")]
        Library,

        /** Actions on the target such as build and test should only be invoked manually
        * and explicitly. For example, triggering a build on all targets in the workspace
        * should by default not include this target.
        * The original motivation to add the "manual" tag comes from a similar functionality
        * that exists in Bazel, where targets with this tag have to be specified explicitly
        * on the command line. */
        [EnumMember(Value="manual")]
        Manual,

        /** Target should be ignored by IDEs. */
        [EnumMember(Value="no-ide")]
        NoIde,

        /** Target contains source code for testing purposes, may have but does not
        * require the `canTest` capability. */
        [EnumMember(Value="test")]
        Test,
    }
}
