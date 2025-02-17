using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using bsp4csharp.Protocol;
using BaseProtocol;

namespace dotnet_bsp.EventHandlers;

public class TestDiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    private AutoResetEvent _waitHandle;
    private readonly BuildTargetIdentifier _buildTarget;
    private readonly string? _originId;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    private readonly TaskId _taskId;

    public TestDiscoveryEventHandler(AutoResetEvent waitHandle, BuildTargetIdentifier buildTarget, string? originId, IBaseProtocolClientManager baseProtocolClientManager)
    {
        _waitHandle = waitHandle;
        _buildTarget = buildTarget;
        _originId = originId;
        _baseProtocolClientManager = baseProtocolClientManager;
        _taskId = new TaskId { Id = Guid.NewGuid().ToString() };

        StartTestCaseDiscovery();
    }

    private void StartTestCaseDiscovery()
    {
        var taskStartParams = new TaskStartParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            Message = "TestCase discovery started",
            EventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            DataKind = TaskStartDataKind.TestCaseDiscoveryTask,
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskStart, taskStartParams, CancellationToken.None);
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases)
    {
        if (discoveredTestCases != null)
        {
            SendTestCaseFoundNotifications(discoveredTestCases);
        }
    }

    private void SendTestCaseFoundNotifications(IEnumerable<TestCase> discoveredTestCases)
    {
        foreach (var testCase in discoveredTestCases)
        {
            var taskProgressParams = new TaskProgressParams
            {
                TaskId = _taskId,
                OriginId = _originId,
                Message = $"TestCase discovered: {testCase.DisplayName}",
                DataKind = TaskProgressDataKind.TestCaseDiscovered,
                Data = new TestCaseDiscoveredData
                {
                    Id = testCase.Id.ToString("N"),
                    BuildTarget = _buildTarget,
                    Source = testCase.Source,
                    FilePath = testCase.CodeFilePath ?? "",
                    DisplayName = testCase.DisplayName,
                    FullyQualifiedName = testCase.FullyQualifiedName,
                    Line = testCase.LineNumber
                }
            };
            _ = _baseProtocolClientManager.SendNotificationAsync(
                Methods.BuildTaskProgress, taskProgressParams, CancellationToken.None);
        }
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            SendTestCaseFoundNotifications(lastChunk);
        }

        var taskFinishParams = new TaskFinishParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            Message = "TestCase discovery finished",
            EventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = StatusCode.Ok,
            DataKind = TaskFinishDataKind.TestCaseDiscoveryFinish,
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
        _waitHandle.Set();
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        var logMessageParams = new LogMessageParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            MessageType = MessageType.Log,
            Message = string.Format("[TestCaseDiscovery log]: {0}", message),
        };
        _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildLogMessage, logMessageParams, CancellationToken.None);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }
}