using bsp4csharp.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;

namespace test;

public class ServerCallbacks(ITestOutputHelper testOutputHelper, LogLevel logLevel = LogLevel.Warning)
{
    private readonly List<Diagnostic> _diagnosticsCollection = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnosticsCollection.AsReadOnly();

    private readonly List<object> _taskNotificationsCollection = [];
    public IReadOnlyCollection<object> TaskNotifications => _taskNotificationsCollection.AsReadOnly();

    private readonly List<LogMessageParams> _logMessagesCollection = [];
    public IReadOnlyCollection<LogMessageParams> LogMessages => _logMessagesCollection.AsReadOnly();

    private readonly LogLevel _logLevel = logLevel;

    [JsonRpcMethod(Methods.BuildPublishDiagnostics, UseSingleObjectParameterDeserialization = true)]
    public Task BuildPublishDiagnosticsRecievedAsync(PublishDiagnosticsParams publishDiagnosticsParams)
    {
        _diagnosticsCollection.AddRange(publishDiagnosticsParams.Diagnostics);
        if (_logLevel >= LogLevel.Information)
        {
            foreach (var diagnostic in publishDiagnosticsParams.Diagnostics)
            {
                testOutputHelper.WriteLine(diagnostic.Message);
            }
        }
        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.BuildLogMessage, UseSingleObjectParameterDeserialization = true)]
    public void BuildLogMessage(LogMessageParams logMessageParams)
    {
        _logMessagesCollection.Add(logMessageParams);
        if (_logLevel >= LogLevel.Information)
        {
            testOutputHelper.WriteLine(logMessageParams.Message);
        }
    }

    [JsonRpcMethod(Methods.BuildTaskStart, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskStartAsync(TaskStartParams taskStartParams)
    {
        var message = string.Format("TaskStart: {0}", taskStartParams.Message);
        if (_logLevel >= LogLevel.Information)
        {
            testOutputHelper.WriteLine(message);
        }
        _taskNotificationsCollection.Add(taskStartParams);
        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.BuildTaskProgress, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskProgressAsync(TaskProgressParams taskProgressParams)
    {
        var message = string.Format("TaskProgress: {0}", taskProgressParams.Message);
        if (_logLevel >= LogLevel.Information)
        {
            testOutputHelper.WriteLine(message);
        }
        _taskNotificationsCollection.Add(taskProgressParams);
        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.BuildTaskFinish, UseSingleObjectParameterDeserialization = true)]
    public Task BuildTaskFinishAsync(TaskFinishParams taskFinishParams)
    {
        var message = string.Format("TaskFinish: {0}", taskFinishParams.Message);
        if (_logLevel >= LogLevel.Information)
        {
            testOutputHelper.WriteLine(message);
        }
        _taskNotificationsCollection.Add(taskFinishParams);
        return Task.CompletedTask;
    }

    // public const string BuildShowMessage = "build/showMessage";
    // public const string BuildTargetDidChange = "buildTarget/didChange";
    // public const string RunPrintStdout = "run/printStdout";
    // public const string RunPrintStderr = "run/printStderr";
}