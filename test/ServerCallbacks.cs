using bsp4csharp.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public class ServerCallbacks(ITestOutputHelper testOutputHelper)
{
    // public const string BuildShowMessage = "build/showMessage";
    // public const string BuildTargetDidChange = "buildTarget/didChange";
    // public const string BuildTaskStart = "build/taskStart";
    // public const string BuildTaskProgress = "build/taskProgress";
    // public const string BuildTaskFinish = "build/taskFinish";
    // public const string RunPrintStdout = "run/printStdout";
    // public const string RunPrintStderr = "run/printStderr";

    [JsonRpcMethod(Methods.BuildPublishDiagnostics, UseSingleObjectParameterDeserialization = true)]
    public Task BuildPublishDiagnosticsRecievedAsync(PublishDiagnosticsParams publishDiagnosticsParams)
    {
        foreach (var diagnostic in publishDiagnosticsParams.Diagnostics)
        {
            testOutputHelper.WriteLine(diagnostic.Message);
        }
        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.BuildLogMessage, UseSingleObjectParameterDeserialization = true)]
    public void BuildLogMessage(LogMessageParams logMessageParams)
    {
        testOutputHelper.WriteLine(logMessageParams.Message);
    }
}