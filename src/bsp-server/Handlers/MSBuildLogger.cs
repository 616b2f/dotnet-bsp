using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Framework;

namespace dotnet_bsp.Handlers
{
    internal class MSBuildLogger : ILogger
    {
        private readonly IBaseProtocolClientManager _baseProtocolClientManager;

        public MSBuildLogger(BaseProtocol.IBaseProtocolClientManager baseProtocolClientManager)
        {
            _baseProtocolClientManager = baseProtocolClientManager;
        }

        public LoggerVerbosity Verbosity { get; set; }
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.WarningRaised += WarningRaised;
            eventSource.ErrorRaised += ErrorRaised;
            eventSource.MessageRaised += MessageRaised;
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            // var diagParams = new PublishDiagnosticsParams
            // {
            //     TextDocument = new TextDocumentIdentifier { Uri = new System.Uri(e.File, UriKind.Absolute) },
            //     BuildTarget = new BuildTargetIdentifier { Uri = new System.Uri(e.ProjectFile, UriKind.Absolute) },
            // };
            // diagParams.Diagnostics.Add(new Diagnostic
            // {
            //     Range = new bsp4csharp.Protocol.Range
            //     {
            //      Start = new Position { Line = e.LineNumber - 1 , Character = e.ColumnNumber - 1 },
            //      End = new Position { Line = e.EndLineNumber - 1, Character = e.EndColumnNumber - 1}
            //     },
            //     Message = e.Message ?? "No message",
            //     Code = e.Code,
            //     Source = e.SenderName,
            //     Severity = DiagnosticSeverity.Information
            // });
            // var _ = _baseProtocolClientManager.SendNotificationAsync(
            //     Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
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
