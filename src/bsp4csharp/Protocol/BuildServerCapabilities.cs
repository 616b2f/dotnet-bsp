using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace bsp4csharp.Protocol;

/// <summary>
/// Class which represents server capabilities.
///
/// See the <see href="https://build-server-protocol.github.io/docs/specification#buildservercapabilities">Build Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
public class BuildServerCapabilities
{
    /// <summary>
    /// The languages the server supports compilation via method buildTarget/compile. 
    /// </summary>
    [DataMember(Name = "compileProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public CompileProvider? CompileProvider { get; set; }

    /// <summary>
    /// The languages the server supports test execution via method buildTarget/test. 
    /// </summary>
    [DataMember(Name = "testProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public TestProvider? TestProvider { get; set; }

    /// <summary>
    /// The languages the server supports run via method buildTarget/run. 
    /// </summary>
    [DataMember(Name = "runProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public RunProvider? RunProvider { get; set; }

    /// <summary>
    /// The languages the server supports debugging via method debugSession/start. 
    /// </summary>
    [DataMember(Name = "debugProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DebugProvider? DebugProvider { get; set; }

    /// <summary>
    /// Theserver can provide a list of targets that contain a
    /// single text document via the method buildTarget/inverseSources 
    /// </summary>
    [DataMember(Name = "inverseSourcesProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? InverseSourcesProvider { get; set; }

    /// <summary>
    /// The server provides sources for library dependencies
    /// via method buildTarget/dependencySources 
    /// </summary>
    [DataMember(Name = "dependencySourcesProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? DependencySourcesProvider { get; set; }

    /// <summary>
    /// The server can provide a list of dependency modules (libraries with meta information)
    /// via method buildTarget/dependencyModules 
    /// </summary>
    [DataMember(Name = "dependencyModulesProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? DependencyModulesProvider { get; set; }

    /// <summary>
    /// The server provides all the resource dependencies
    /// via method buildTarget/resources 
    /// </summary>
    [DataMember(Name = "resourcesProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? ResourcesProvider { get; set; }

    /// <summary>
    /// The server provides all output paths
    /// via method buildTarget/outputPaths 
    /// </summary>
    [DataMember(Name = "outputPathsProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? OutputPathsProvider { get; set; }

    /// <summary>
    /// The server sends notifications to the client on build
    /// target change events via buildTarget/didChange 
    /// </summary>
    [DataMember(Name = "buildTargetChangedProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? BuildTargetChangedProvider { get; set; }

    /// <summary>
    /// The server can respond to `buildTarget/jvmRunEnvironment` requests with the
    /// necessary information required to launch a Java process to run a main class. 
    /// </summary>
    [DataMember(Name = "jvmRunEnvironmentProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? JvmRunEnvironmentProvider { get; set; }

    /*/// The server can respond to `buildTarget/jvmTestEnvironment` requests with the
    /// necessary information required to launch a Java process for testing or
    /// debugging. */
    [DataMember(Name = "jvmTestEnvironmentProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? JvmTestEnvironmentProvider { get; set; }

    /// <summary>
    /// The server can respond to `workspace/cargoFeaturesState` and
    /// `setCargoFeatures` requests. In other words, supports Cargo Features extension. 
    /// </summary>
    [DataMember(Name = "cargoFeaturesProvider")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? CargoFeaturesProvider { get; set; }

    /// <summary>
    /// Reloading the build state through workspace/reload is supported 
    /// </summary>
    [DataMember(Name = "canReload")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? CanReload { get; set; }
}
