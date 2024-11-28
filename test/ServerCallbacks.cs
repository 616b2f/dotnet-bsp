using bsp4csharp.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public class ServerCallbacks(ITestOutputHelper testOutputHelper)
{
    private readonly List<Diagnostic> _diagnosticsCollection = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnosticsCollection.AsReadOnly();

    [JsonRpcMethod(Methods.BuildPublishDiagnostics, UseSingleObjectParameterDeserialization = true)]
    public Task BuildPublishDiagnosticsRecievedAsync(PublishDiagnosticsParams publishDiagnosticsParams)
    {
        _diagnosticsCollection.AddRange(publishDiagnosticsParams.Diagnostics);
        foreach (var diagnostic in publishDiagnosticsParams.Diagnostics)
        {
            testOutputHelper.WriteLine(diagnostic.Message);
        }
        return Task.CompletedTask;
    }

    private readonly List<LogMessageParams> _logMessagesCollection = [];
    public IReadOnlyCollection<LogMessageParams> LogMessages => _logMessagesCollection.AsReadOnly();

    [JsonRpcMethod(Methods.BuildLogMessage, UseSingleObjectParameterDeserialization = true)]
    public void BuildLogMessage(LogMessageParams logMessageParams)
    {
        _logMessagesCollection.Add(logMessageParams);
        testOutputHelper.WriteLine(logMessageParams.Message);
    }

    private readonly List<object> _taskNotificationsCollection = [];
    public IReadOnlyCollection<object> TaskNotifications => _taskNotificationsCollection.AsReadOnly();

    [JsonRpcMethod(Methods.BuildTaskStart, UseSingleObjectParameterDeserialization = true)]
    public void BuildTaskStart(TaskStartParams taskStartParams)
    {
        var message = string.Format("TaskStart: {0}", taskStartParams.Message);
        testOutputHelper.WriteLine(message);
        _taskNotificationsCollection.Add(taskStartParams);
    }

    [JsonRpcMethod(Methods.BuildTaskProgress, UseSingleObjectParameterDeserialization = true)]
    public void BuildTaskProgress(TaskProgressParams taskProgressParams)
    {
        var message = string.Format("TaskProgress: {0}", taskProgressParams.Message);
        testOutputHelper.WriteLine(message);
        _taskNotificationsCollection.Add(taskProgressParams);
    }

    [JsonRpcMethod(Methods.BuildTaskFinish, UseSingleObjectParameterDeserialization = true)]
    public void BuildTaskFinish(TaskFinishParams taskFinishParams)
    {
        var message = string.Format("TaskFinish: {0}", taskFinishParams.Message);
        testOutputHelper.WriteLine(message);
        _taskNotificationsCollection.Add(taskFinishParams);
    }
    // public const string BuildTaskFinish = "build/taskFinish";

    // public const string BuildShowMessage = "build/showMessage";
    // public const string BuildTargetDidChange = "buildTarget/didChange";
    // public const string RunPrintStdout = "run/printStdout";
    // public const string RunPrintStderr = "run/printStderr";
}