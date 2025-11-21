using System.Runtime.Serialization;

namespace bsp4csharp.Protocol;

public enum BuildTargetDataKind
{
    /** `data` field must contain a CargoBuildTarget object. */
    [EnumMember(Value="cargo")]
    Cargo,

    /** `data` field must contain a CppBuildTarget object. */
    [EnumMember(Value="cpp")]
    Cpp,

    /** `data` field must contain a JvmBuildTarget object. */
    [EnumMember(Value="jvm")]
    Jvm,

    /** `data` field must contain a PythonBuildTarget object. */
    [EnumMember(Value="python")]
    Python,

    /** `data` field must contain a SbtBuildTarget object. */
    [EnumMember(Value="sbt")]
    Sbt,

    /** `data` field must contain a ScalaBuildTarget object. */
    [EnumMember(Value="scala")]
    Scala,
}