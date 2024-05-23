using BaseProtocol;
using BaseProtocol.Protocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Framework;

namespace dotnet_bsp.Logging
{
    internal class MSBuildLogger : ILogger
    {
        private readonly IBaseProtocolClientManager _baseProtocolClientManager;
        private readonly string _workspacePath;
        private readonly ICollection<string> _diagnosticKeysCollection = [];

        public MSBuildLogger(BaseProtocol.IBaseProtocolClientManager baseProtocolClientManager, string workspacePath)
        {
            _baseProtocolClientManager = baseProtocolClientManager;
            _workspacePath = workspacePath;
            Parameters = string.Empty;
        }

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            CleanDiagnostics();

            eventSource.BuildStarted += BuildStarted;
            eventSource.BuildFinished += BuildFinished;
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;

            eventSource.TaskStarted += TaskStartedRaised;
            eventSource.TaskFinished += TaskFinishedRaised;

            eventSource.MessageRaised += MessageRaised;
            eventSource.WarningRaised += WarningRaised;
            eventSource.ErrorRaised += ErrorRaised;
        }

        private void CleanDiagnostics()
        {
            var diagParams = new PublishDiagnosticsParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = UriWithSchema("/") },
                BuildTarget = new BuildTargetIdentifier { Uri = UriWithSchema("/") },
                Reset = true
            };

            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            if (e.BuildEventContext?.BuildRequestId != null)
            {
                var taskStartParams = new TaskStartParams
                {
                    TaskId = new TaskId { Id = e.BuildEventContext!.BuildRequestId.ToString() },
                    Message = e.Message,
                    EventTime = e.Timestamp.Millisecond,
                };
                _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
            }
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            if (e.BuildEventContext?.BuildRequestId != null)
            {
                var taskFinishParams = new TaskFinishParams
                {
                    TaskId = new TaskId { Id = e.BuildEventContext!.BuildRequestId.ToString() },
                    Message = e.Message,
                    EventTime = e.Timestamp.Millisecond,
                    Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
            }
       }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            if (e.BuildEventContext?.ProjectContextId != null &&
                e.BuildEventContext?.ProjectContextId != BuildEventContext.InvalidProjectContextId)
            {
                var taskStartParams = new TaskStartParams
                {
                    TaskId = new TaskId { Id = e.ProjectFile!, Parents = [e.BuildEventContext!.BuildRequestId.ToString()] },
                    Message = "[" + e.SenderName ?? "MSBUILD" + "]: " + e.Message + "; " + e.ParentProjectBuildEventContext?.GetHashCode().ToString(),
                    EventTime = e.Timestamp.Millisecond
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
            }
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            if (e.BuildEventContext?.ProjectContextId != null &&
                e.BuildEventContext?.ProjectContextId != BuildEventContext.InvalidProjectContextId)
            {
                var taskFinishParams = new TaskFinishParams
                {
                    TaskId = new TaskId { Id = e.ProjectFile!, Parents = [e.BuildEventContext!.BuildRequestId.ToString()] },
                    Message = "[" + e.SenderName ?? "MSBUILD" + "]: " + e.Message + "; " + e.BuildEventContext?.GetHashCode().ToString(),
                    EventTime = e.Timestamp.Millisecond,
                    Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
            }
        }

        private void TaskStartedRaised(object sender, TaskStartedEventArgs e)
        {
            var projectFile = e.ProjectFile.StartsWith(_workspacePath) ? e.ProjectFile.Substring(_workspacePath.Length + 1) : e.ProjectFile;
            var taskProgressParams = new TaskProgressParams
            {
                TaskId = new TaskId { Id = e.ProjectFile },
                Message = "[" + e.TaskName + "][" + projectFile + "]:" + e.Message ?? "",
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }

        private void TaskFinishedRaised(object sender, TaskFinishedEventArgs e)
        {
            var projectFile = e.ProjectFile.StartsWith(_workspacePath) ? e.ProjectFile.Substring(_workspacePath.Length + 1) : e.ProjectFile;
            var taskProgressParams = new TaskProgressParams
            {
                TaskId = new TaskId { Id = e.ProjectFile },
                Message = "[" + e.TaskName + "][" + projectFile + "]:" + e.Message ?? "",
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (
                (e.Importance == MessageImportance.High && Verbosity >= LoggerVerbosity.Detailed) ||
                (e.Importance == MessageImportance.Normal && Verbosity <= LoggerVerbosity.Normal && Verbosity > LoggerVerbosity.Quiet) ||
                (e.Importance == MessageImportance.Low && Verbosity == LoggerVerbosity.Quiet)
            )
            {
                var logMessgeParams = new LogMessageParams
                {
                    Message = "[" + e.SenderName + "]: " + e.Message ?? "",
                    MessageType = MessageType.Log
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);

                var taskProgressParams = new TaskProgressParams
                {
                    TaskId = new TaskId { Id = e.ThreadId.ToString() },
                    Message = "[" + e.SenderName + "][" + e.Importance.ToString() + "]: " + e.Message ?? "",
                };
                _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
            }
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            var diagParams = new PublishDiagnosticsParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = UriWithSchema(e.File) },
                BuildTarget = new BuildTargetIdentifier { Uri = UriWithSchema(e.ProjectFile) },
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
                    Start = new Position { Line = e.LineNumber - 1 , Character = e.ColumnNumber - 1 },
                    End = new Position { Line = e.EndLineNumber - 1, Character = e.EndColumnNumber - 1}
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
                TextDocument = new TextDocumentIdentifier { Uri = UriWithSchema(e.File) },
                BuildTarget = new BuildTargetIdentifier { Uri = UriWithSchema(e.ProjectFile) },
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
                    Start = new Position { Line = e.LineNumber - 1 , Character = e.ColumnNumber - 1 },
                    End = new Position { Line = e.EndLineNumber - 1, Character = e.EndColumnNumber - 1}
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

        private Uri UriWithSchema(string path)
        {
            // workaround for "file://" schema being not serialized: https://github.com/dotnet/runtime/issues/90140
            return new Uri($"file://{path}", UriKind.Absolute);
        }
    }
}