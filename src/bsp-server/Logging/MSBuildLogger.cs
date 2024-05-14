using BaseProtocol;
using BaseProtocol.Protocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Framework;

namespace dotnet_bsp.Logging
{
    internal class MSBuildLogger : ILogger
    {
        private readonly IBaseProtocolClientManager _baseProtocolClientManager;

        public MSBuildLogger(BaseProtocol.IBaseProtocolClientManager baseProtocolClientManager)
        {
            _baseProtocolClientManager = baseProtocolClientManager;
            Parameters = string.Empty;
        }

        public LoggerVerbosity Verbosity { get; set; }
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            // eventSource.TaskStarted += TaskStartedRaised;
            // eventSource.TaskFinished += TaskFinishedRaised;
            // eventSource.BuildStarted += BuildStarted;
            // eventSource.BuildFinished += BuildFinished;
            // eventSource.ProjectStarted += ProjectStarted;
            // eventSource.ProjectFinished += ProjectFinished ;
            eventSource.MessageRaised += MessageRaised;
            eventSource.WarningRaised += WarningRaised;
            eventSource.ErrorRaised += ErrorRaised;
            // eventSource.StatusEventRaised += StatusEventRaised;
            // eventSource.AnyEventRaised += AnyEventRaised;
        }

        private void StatusEventRaised(object sender, BuildStatusEventArgs e)
        {
        }

        private void AnyEventRaised(object sender, BuildEventArgs e)
        {
            // if (e is BuildStatusEventArgs a)
            // {
            //     var logMessgeParams = new LogMessageParams
            //     {
            //         Message = a.BuildEventContext. + ": " + a.Message ?? "",
            //         MessageType = MessageType.Log
            //     };
            //     var _ = _baseProtocolClientManager.SendNotificationAsync(
            //         Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
            // }
            // else
            // {
                var logMessgeParams = new LogMessageParams
                {
                    Message = e.SenderName + ": " + e.Timestamp + ": " + e.Message ?? "",
                    MessageType = MessageType.Log
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
            //}
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            var taskStartParams = new TaskStartParams
            {
                TaskId = new TaskId { Id = e.ProjectFile + ":" + e.ThreadId.ToString() },
                Message = e.SenderName + ": " + e.Message + "; " + e.ParentProjectBuildEventContext?.GetHashCode().ToString(),
                EventTime = e.Timestamp.Millisecond
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            var taskFinishParams = new TaskFinishParams
            {
                TaskId = new TaskId { Id = e.ProjectFile + ":" + e.ThreadId.ToString() },
                Message = e.SenderName + ": " + e.Message + "; " + e.BuildEventContext?.GetHashCode().ToString(),
                EventTime = e.Timestamp.Millisecond,
                Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            var taskStartParams = new TaskStartParams
            {
                TaskId = new TaskId { Id = e.BuildEventContext?.TaskId + ":" + e.ThreadId.ToString() },
                Message = e.Message,
                EventTime = e.Timestamp.Millisecond
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            var taskFinishParams = new TaskFinishParams
            {
                TaskId = new TaskId { Id = e.BuildEventContext?.TaskId + ":" + e.ThreadId.ToString() },
                Message = e.Message,
                EventTime = e.Timestamp.Millisecond,
                Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
       }

        private void TaskStartedRaised(object sender, TaskStartedEventArgs e)
        {
            var taskStartParams = new TaskStartParams
            {
                TaskId = new TaskId { Id = e.ProjectFile + ":" + e.TaskName + ":" + e.ThreadId },
                Message = e.ProjectFile + ":" + e.TaskName + ":" + e.Message,
                EventTime = e.Timestamp.Millisecond
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
        }

        private void TaskFinishedRaised(object sender, TaskFinishedEventArgs e)
        {
            var taskFinishParams = new TaskFinishParams
            {
                TaskId = new TaskId { Id = e.ProjectFile + ":" + e.TaskName },
                Message = e.ProjectFile + ":" + e.TaskName + ":" + e.Message,
                EventTime = e.Timestamp.Millisecond,
                Status = e.Succeeded ? StatusCode.Ok : StatusCode.Error,
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            var logMessgeParams = new LogMessageParams
            {
                Message = e.Subcategory + ": " + e.Message ?? "",
                MessageType = MessageType.Log
            };
            var _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            var diagParams = new PublishDiagnosticsParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = new System.Uri(e.File, UriKind.Absolute) },
                BuildTarget = new BuildTargetIdentifier { Uri = new System.Uri(e.ProjectFile, UriKind.Absolute) },
            };
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
                TextDocument = new TextDocumentIdentifier { Uri = new System.Uri(e.File, UriKind.Absolute) },
                BuildTarget = new BuildTargetIdentifier { Uri = new System.Uri(e.ProjectFile, UriKind.Absolute) },
            };
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
    }
}
