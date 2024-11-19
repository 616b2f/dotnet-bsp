using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Methods = bsp4csharp.Protocol.Methods;

namespace dotnet_bsp.Logging;

internal class MSBuildLogger(IBaseProtocolClientManager baseProtocolClientManager, string? originId, string workspacePath, string buildTarget) : ILogger
{
    private readonly IBaseProtocolClientManager _baseProtocolClientManager = baseProtocolClientManager;
    private readonly string _workspacePath = workspacePath;
    private readonly string _buildTarget = buildTarget;
    private readonly string _taskId = Guid.NewGuid().ToString();
    private readonly string? _originId = originId;
    private readonly ICollection<string> _diagnosticKeysCollection = [];
    private int Warnings = 0;
    private int Errors = 0;
    private DateTime _buildStartTimestamp;
    private readonly string[] _targetsOfInterest = ["Clean", "Build", "Restore"];

    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;

    public string Parameters { get; set; } = string.Empty;

    public void Initialize(IEventSource eventSource)
    {
        // eventSource.BuildStarted += BuildStarted;
        // eventSource.BuildFinished += BuildFinished;
        // eventSource.ProjectStarted += ProjectStarted;
        // eventSource.ProjectFinished += ProjectFinished;

        eventSource.TargetStarted += TargetStarted;
        eventSource.TargetFinished += TargetFinished;

        eventSource.TaskStarted += TaskStartedRaised;
        eventSource.TaskFinished += TaskFinishedRaised;

        eventSource.MessageRaised += MessageRaised;
        eventSource.WarningRaised += WarningRaised;
        eventSource.ErrorRaised += ErrorRaised;
    }

    private void TargetStarted(object sender, TargetStartedEventArgs e)
    {
        if (_targetsOfInterest.Contains(e.TargetName, StringComparer.InvariantCultureIgnoreCase) &&
            e.BuildReason == TargetBuiltReason.None)
        {
            var taskId = new TaskId { Id = e.ProjectFile + "#" + e.TargetName + "#" + e.ThreadId };
            var taskStartParams = new TaskStartParams
            {
                TaskId = taskId,
                OriginId = _originId,
                Message = $"[{e.TargetName}]: {e.ProjectFile}",
                EventTime = e.Timestamp.Millisecond,
                DataKind = TaskStartDataKind.CompileTask,
                Data = new CompileTask
                {
                    Target = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(e.ProjectFile) }
                }
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskStart, taskStartParams, CancellationToken.None);

            var logMessgeParams = new LogMessageParams
            {
                Message = e.Message,
                MessageType = MessageType.Log
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
        }
    }

    private void TargetFinished(object sender, TargetFinishedEventArgs e)
    {
        if (_targetsOfInterest.Contains(e.TargetName, StringComparer.InvariantCultureIgnoreCase))
        {
            var taskId = new TaskId { Id = e.ProjectFile + "#" + e.TargetName + "#" + e.ThreadId };
            var taskFinishParams = new TaskFinishParams
            {
                TaskId = taskId,
                OriginId = _originId,
                Message = $"[{e.TargetName}]: {e.ProjectFile}",
                EventTime = e.Timestamp.Millisecond,
                Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
                DataKind = TaskFinishDataKind.CompileReport,
                Data = new CompileReport
                {
                    Target = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(e.ProjectFile) },
                    Warnings = Warnings,
                    Errors = Errors,
                    Time = Convert.ToInt64((e.Timestamp - _buildStartTimestamp).TotalMilliseconds),
                }
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);

            var logMessgeParams = new LogMessageParams
            {
                Message = e.Message,
                MessageType = MessageType.Log
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
        }
    }


    private void BuildStarted(object sender, BuildStartedEventArgs e)
    {
        var taskId = new TaskId { Id = _taskId };
        var taskStartParams = new TaskStartParams
        {
            TaskId = taskId,
            OriginId = _originId,
            Message = e.Message + ": " + _buildTarget,
            EventTime = e.Timestamp.Millisecond,
            DataKind = TaskStartDataKind.CompileTask,
            Data = new CompileTask
            {
                Target = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(_buildTarget) }
            }
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskStart, taskStartParams, CancellationToken.None);

        _buildStartTimestamp = e.Timestamp;

        var message = "[BuildStartEvent]:" + JsonConvert.SerializeObject(e);
        var logMessgeParams = new LogMessageParams
        {
            Message = message,
            MessageType = MessageType.Log
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
    }

    private void BuildFinished(object sender, BuildFinishedEventArgs e)
    {
        var taskId = new TaskId { Id = _taskId };
        var taskFinishParams = new TaskFinishParams
        {
            TaskId = taskId,
            OriginId = _originId,
            Message = e.Message + ": " + _buildTarget,
            EventTime = e.Timestamp.Millisecond,
            Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
            DataKind = TaskFinishDataKind.CompileReport,
            Data = new CompileReport
            {
                Target = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(_buildTarget) },
                Warnings = Warnings,
                Errors = Errors,
                Time = Convert.ToInt64((e.Timestamp - _buildStartTimestamp).TotalMilliseconds),
            }
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
   }

    private void ProjectStarted(object sender, ProjectStartedEventArgs e)
    {
        var taskId = new TaskId { Id = e.ProjectFile! };
        if (e.BuildEventContext?.ProjectContextId != null &&
            e.BuildEventContext?.ProjectContextId != BuildEventContext.InvalidProjectContextId)
        {
            taskId.Parents = [e.BuildEventContext!.BuildRequestId.ToString()];
        }

        var taskStartParams = new TaskStartParams
        {
            TaskId = taskId,
            OriginId = _originId,
            Message = "[" + (e.SenderName ?? "MSBUILD") + "]: " + e.Message,
            EventTime = e.Timestamp.Millisecond
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
    }

    private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
    {
        var taskId = new TaskId { Id = e.ProjectFile! };
        if (e.BuildEventContext?.ProjectContextId != null &&
            e.BuildEventContext?.ProjectContextId != BuildEventContext.InvalidProjectContextId)
        {
            taskId.Parents = [e.BuildEventContext!.BuildRequestId.ToString()];
        }

        var taskFinishParams = new TaskFinishParams
        {
            TaskId = taskId,
            OriginId = _originId,
            Message = "[" + (e.SenderName ?? "MSBUILD") + "]: " + e.Message,
            EventTime = e.Timestamp.Millisecond,
            Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
    }

    private void TaskStartedRaised(object sender, TaskStartedEventArgs e)
    {
        if (Verbosity > LoggerVerbosity.Normal)
        {
            var projectFile = GetRelativeProjectPathToWorkspace(e.ProjectFile);
            var taskProgressParams = new TaskProgressParams
            {
                TaskId = new TaskId { Id = e.ProjectFile },
                OriginId = _originId,
                Message = "[" + e.TaskName + "][" + projectFile + "]:" + e.Message ?? "",
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }
    }

    private void TaskFinishedRaised(object sender, TaskFinishedEventArgs e)
    {
        if (Verbosity > LoggerVerbosity.Normal)
        {
            var projectFile = GetRelativeProjectPathToWorkspace(e.ProjectFile);
            var taskProgressParams = new TaskProgressParams
            {
                TaskId = new TaskId { Id = e.ProjectFile },
                OriginId = _originId,
                Message = "[" + e.TaskName + "][" + projectFile + "]:" + e.Message ?? "",
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }
    }

    private string GetRelativeProjectPathToWorkspace(string projectFile)
    {
        return projectFile.StartsWith(_workspacePath) ? projectFile.Substring(_workspacePath.Length + 1) : projectFile;
    }

    private void MessageRaised(object sender, BuildMessageEventArgs e)
    {
        if (
            (e.Importance == MessageImportance.High && Verbosity >= LoggerVerbosity.Detailed) ||
            (e.Importance == MessageImportance.Normal && Verbosity <= LoggerVerbosity.Normal && Verbosity > LoggerVerbosity.Quiet) ||
            (e.Importance == MessageImportance.Low && Verbosity == LoggerVerbosity.Quiet)
        )
        {
            var message = "[" + e.SenderName + "][" + e.Importance.ToString() + "]: " + e.Message ?? "";
            var logMessgeParams = new LogMessageParams
            {
                Message = message,
                MessageType = MessageType.Log
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);

            var taskProgressParams = new TaskProgressParams
            {
                TaskId = new TaskId { Id = e.ThreadId.ToString() },
                OriginId = _originId,
                Message = message,
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }
    }

    private void ErrorRaised(object sender, BuildErrorEventArgs e)
    {
        var diagParams = new PublishDiagnosticsParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = UriFixer.WithFileSchema(e.File) },
            BuildTarget = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(e.ProjectFile) },
        };

        var key = string.Concat(diagParams.TextDocument.Uri, "|", diagParams.BuildTarget.Uri);
        diagParams.Reset = !_diagnosticKeysCollection.Contains(key);
        if (diagParams.Reset)
        {
            _diagnosticKeysCollection.Add(key);
        }

        diagParams.Diagnostics.Add(new Diagnostic
        {
            Range = new bsp4csharp.Protocol.Range
            {
                Start = new Position { Line = e.LineNumber - 1, Character = e.ColumnNumber - 1 },
                End = new Position { Line = e.EndLineNumber - 1, Character = e.EndColumnNumber - 1 }
            },
            Message = e.Message ?? "No message",
            Code = e.Code,
            Source = e.SenderName,
            Severity = DiagnosticSeverity.Error
        });
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
    }

    private void WarningRaised(object sender, BuildWarningEventArgs e)
    {
        var diagParams = new PublishDiagnosticsParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = UriFixer.WithFileSchema(e.File) },
            BuildTarget = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(e.ProjectFile) },
        };

        var key = string.Concat(diagParams.TextDocument.Uri, "|", diagParams.BuildTarget.Uri);
        diagParams.Reset = !_diagnosticKeysCollection.Contains(key);
        if (diagParams.Reset)
        {
            _diagnosticKeysCollection.Add(key);
        }

        diagParams.Diagnostics.Add(new Diagnostic
        {
            Range = new bsp4csharp.Protocol.Range
            {
                Start = new Position { Line = e.LineNumber - 1, Character = e.ColumnNumber - 1 },
                End = new Position { Line = e.EndLineNumber - 1, Character = e.EndColumnNumber - 1 }
            },
            Message = e.Message ?? "No message",
            Code = e.Code,
            Source = e.SenderName,
            Severity = DiagnosticSeverity.Warning
        });
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
    }

    public void Shutdown()
    {
    }
}