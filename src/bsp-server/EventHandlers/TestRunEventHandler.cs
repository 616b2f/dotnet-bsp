using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using BaseProtocol;
using bsp4csharp.Protocol;
using System.Collections.ObjectModel;

namespace dotnet_bsp.EventHandlers;

public class TestRunEventHandler : ITestRunEventsHandler
{
    private AutoResetEvent _waitHandle;
    private readonly string? _originId;
    private readonly BuildTargetIdentifier _buildTargetIdentifier;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;
    private TaskId _taskId;

    public TestRunEventHandler(
        AutoResetEvent waitHandle,
        string? originId,
        BuildTargetIdentifier buildTargetIdentifier,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _waitHandle = waitHandle;
        _originId = originId;
        _buildTargetIdentifier = buildTargetIdentifier;
        _baseProtocolClientManager = baseProtocolClientManager;
        _taskId = new TaskId { Id = Guid.NewGuid().ToString() };

        StartTestRun();
    }

    private void StartTestRun()
    {
        var taskStartParams = new TaskStartParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            Message = "Test run started",
            EventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            DataKind = TaskStartDataKind.TestTask,
            Data = new TestTaskData
            {
                Target = _buildTargetIdentifier
            }
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        var logMessageParams = new LogMessageParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            MessageType = MessageType.Log,
            Message = string.Format("[Test log]: {0}", message),
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildLogMessage, logMessageParams, CancellationToken.None);
    }

    private void WriteDiagnostic(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result)
    {
        var diagParams = new PublishDiagnosticsParams
        {
            BuildTarget = _buildTargetIdentifier,
            TextDocument = new TextDocumentIdentifier { Uri = UriFixer.WithFileSchema(result.TestCase.CodeFilePath ?? "") },
        };

        var key = string.Concat(diagParams.TextDocument.Uri, "|", diagParams.BuildTarget.Uri);
        // diagParams.Reset = !_diagnosticKeysCollection.Contains(key);
        // if (diagParams.Reset)
        // {
        //     _diagnosticKeysCollection.Add(key);
        // }

        var testCase = result.TestCase;
        diagParams.Diagnostics.Add(new Diagnostic
        {
            Range = new bsp4csharp.Protocol.Range
            {
                Start = new Position { Line = testCase.LineNumber , Character = 0 },
                End = new Position { Line = testCase.LineNumber, Character = 0 }
            },
            Message = string.Format("{0}\n{1}", result.Messages, result.ErrorStackTrace),
            Code = result.TestCase.CodeFilePath,
            Source = testCase.Source,
            Severity = DiagnosticSeverity.Error
        });
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
    }

    public void HandleTestRunComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {
        WriteTestRunProgress(lastChunkArgs);

        var status = StatusCodeFromTestStatus(testRunCompleteArgs);
        var taskFinishParams = new TaskFinishParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            DataKind = TaskFinishDataKind.TestReport,
            Data = new TestReportData
            {
                Target = _buildTargetIdentifier,
                Ignored = 0,
                Cancelled = 0,
                Passed = GetTestOutcomeCount(testRunCompleteArgs, TestOutcome.Passed),
                Failed = GetTestOutcomeCount(testRunCompleteArgs, TestOutcome.Failed),
                Skipped = GetTestOutcomeCount(testRunCompleteArgs, TestOutcome.Skipped),
                Time = Convert.ToInt64(testRunCompleteArgs.ElapsedTimeInRunningTests.TotalMicroseconds),
            },
            Message = status == StatusCode.Error ? testRunCompleteArgs.Error!.Message : "Test run finished",
            EventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = status,
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
        _waitHandle.Set();
    }

    private int GetTestOutcomeCount(TestRunCompleteEventArgs testRunCompleteArgs, TestOutcome key)
    {
        var dict = testRunCompleteArgs.TestRunStatistics?.Stats;
        if (dict is not null && dict.ContainsKey(key))
        {
            return (int)dict[key];
        }
        return 0;
    }

    private StatusCode StatusCodeFromTestStatus(TestRunCompleteEventArgs testRunCompleteArgs)
    {
        if (testRunCompleteArgs.Error is not null)
        {
            return StatusCode.Error;
        }
        else if (testRunCompleteArgs.IsAborted || testRunCompleteArgs.IsCanceled)
        {
            return StatusCode.Cancelled;
        }
        else
        {
            return StatusCode.Ok;
        }
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        WriteTestRunProgress(testRunChangedArgs);
    }

    private void WriteTestRunProgress(TestRunChangedEventArgs? testRunChangedArgs)
    {
        if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
        {
            foreach (var testResult in testRunChangedArgs.NewTestResults)
            {
                var taskProgressParams = new TaskProgressParams
                {
                    TaskId = _taskId,
                    OriginId = _originId,
                    Message = $"{testResult.DisplayName}: {testResult.Outcome}",
                    Unit = "tests",
                    Progress = testRunChangedArgs.TestRunStatistics?.ExecutedTests,
                    Total = testRunChangedArgs.TestRunStatistics?.Stats?.Values.Sum(x => x),
                };
                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);


                var testTaskId = new TaskId
                {
                    Id = testResult.TestCase.FullyQualifiedName,
                    Parents = [_taskId.Id]
                };
                var location = new Location
                {
                    Uri = UriFixer.WithFileSchema(testResult.TestCase.CodeFilePath ?? ""),
                    Range = new bsp4csharp.Protocol.Range
                    {
                        Start = new Position { Line = testResult.TestCase.LineNumber },
                        End = new Position { Line = testResult.TestCase.LineNumber }
                    }
                };
                var taskTestStart = new TaskStartParams
                {
                    TaskId = testTaskId,
                    OriginId = _originId,
                    DataKind = TaskStartDataKind.TestStart,
                    Data = new TaskStartData
                    {
                        DisplayName = testResult.TestCase.FullyQualifiedName,
                        Location = location,
                    },
                    Message = $"Run Test: {testResult.TestCase.FullyQualifiedName}",
                    EventTime = testResult.StartTime.ToUnixTimeMilliseconds(),
                };
                _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskFinish, taskTestStart, CancellationToken.None);
                var taskTestFinish = new TaskFinishParams
                {
                    TaskId = testTaskId,
                    OriginId = _originId,
                    DataKind = TaskFinishDataKind.TestFinish,
                    Data = new TestFinishData
                    {
                        DisplayName = testResult.TestCase.FullyQualifiedName,
                        Message = CombineTestMessages(testResult.Messages, testResult.ErrorMessage, testResult.ErrorStackTrace),
                        Status = ConvertTestOutcome(testResult.Outcome),
                        Location = location
                    },
                    Message = $"Test run finished: {testResult.TestCase.FullyQualifiedName}",
                    EventTime = testResult.EndTime.ToUnixTimeMilliseconds(),
                };
                _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildTaskFinish, taskTestFinish, CancellationToken.None);
            }
        }
    }

    private TestStatus ConvertTestOutcome(TestOutcome outcome)
    {
        return outcome switch
        {
            TestOutcome.None => TestStatus.Ignored,
            TestOutcome.NotFound => TestStatus.Ignored,
            TestOutcome.Failed => TestStatus.Failed,
            TestOutcome.Passed => TestStatus.Passed,
            TestOutcome.Skipped => TestStatus.Skipped,
            _ => throw new Exception($"Mapping was not defined for TestOutcome: {outcome}")
        };
    }

    private string CombineTestMessages(
        Collection<TestResultMessage> messages,
        string? errorMessage,
        string? errorStackTrace)
    {
        var texts = messages
            .Where(x =>
                !string.IsNullOrEmpty(x.Text) &&
                (x.Category == TestResultMessage.StandardOutCategory ||
                 x.Category == TestResultMessage.StandardErrorCategory))
            .Select(x => x.Text)
            .ToList();

        if (errorMessage is not null)
        {
            texts.Add(errorMessage);
        }

        if (errorStackTrace is not null)
        {
            texts.Add(errorStackTrace);
        }

        return string.Join("\n", texts);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }

    public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
    {
        // No op
        return -1;
    }
}