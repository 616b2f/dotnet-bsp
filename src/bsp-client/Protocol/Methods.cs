namespace bsp4csharp.Protocol;

/// <summary>
/// Class which contains the string values for all common build server protocol methods.
/// </summary>
public static class Methods
{
    // server methods
    public const string BuildInitialize = "build/initialize";
    public const string BuildInitialized = "build/initialized";
    public const string BuildShutdown = "build/shutdown";
    public const string BuildExit = "build/exit";
    public const string WorkspaceBuildTargets = "workspace/buildTargets";
    public const string WorkspaceReload = "workspace/reload";
    public const string BuildTargetSources = "buildTarget/sources";
    public const string BuildTargetInverseSources = "buildTarget/inverseSources";
    public const string BuildTargetDependencySources = "buildTarget/dependencySources";
    public const string BuildTargetDependencyModules = "buildTarget/dependencyModules";
    public const string BuildTargetResources = "buildTarget/resources";
    public const string BuildTargetOutputPaths = "buildTarget/outputPaths";
    public const string BuildTargetCompile = "buildTarget/compile";
    public const string BuildTargetRun = "buildTarget/run";
    public const string BuildTargetTest = "buildTarget/test";
    public const string BuildTargetTestCaseDiscovery = "buildTarget/testCaseDiscovery";
    public const string BuildTargetCleanCache = "buildTarget/cleanCache";
    public const string DebugSessionStart = "debugSession/start";
    public const string RunReadStdin = "run/readStdin";

    // client methods
    public const string BuildShowMessage = "build/showMessage";
    public const string BuildLogMessage = "build/logMessage";
    public const string BuildPublishDiagnostics = "build/publishDiagnostics";
    public const string BuildTargetDidChange = "buildTarget/didChange";
    public const string BuildTaskStart = "build/taskStart";
    public const string BuildTaskProgress = "build/taskProgress";
    public const string BuildTaskFinish = "build/taskFinish";
    public const string RunPrintStdout = "run/printStdout";
    public const string RunPrintStderr = "run/printStderr";
}