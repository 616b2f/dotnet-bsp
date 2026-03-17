using bsp4csharp.Protocol;
using StreamJsonRpc;
using System.Threading.Tasks;

namespace bsp_client;

public interface IServerCallbacks
{
    [JsonRpcMethod(Methods.BuildPublishDiagnostics, UseSingleObjectParameterDeserialization = true)]
    public Task BuildPublishDiagnosticsRecievedAsync(PublishDiagnosticsParams publishDiagnosticsParams);

    [JsonRpcMethod(Methods.BuildLogMessage, UseSingleObjectParameterDeserialization = true)]
    public void BuildLogMessage(LogMessageParams logMessageParams);

    [JsonRpcMethod(Methods.BuildTaskStart, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskStartAsync(TaskStartParams taskStartParams);

    [JsonRpcMethod(Methods.BuildTaskProgress, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskProgressAsync(TaskProgressParams taskProgressParams);

    [JsonRpcMethod(Methods.BuildTaskFinish, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskFinishAsync(TaskFinishParams taskFinishParams);

    // public const string BuildShowMessage = "build/showMessage";
    // public const string BuildTargetDidChange = "buildTarget/didChange";
    // public const string RunPrintStdout = "run/printStdout";
    // public const string RunPrintStderr = "run/printStderr";
}