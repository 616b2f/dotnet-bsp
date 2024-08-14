using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using bsp4csharp.Protocol;
using BaseProtocol;

namespace dotnet_bsp.Handlers;

public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    private AutoResetEvent waitHandle;
    private readonly string? _originId;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    private readonly TaskId _taskId;

    public DiscoveryEventHandler(AutoResetEvent waitHandle, string? originId, IBaseProtocolClientManager baseProtocolClientManager)
    {
        this.waitHandle = waitHandle;
        this._originId = originId;
        this._baseProtocolClientManager = baseProtocolClientManager;

        _taskId = new TaskId { Id = Guid.NewGuid().ToString() };
        StartTestCaseDiscovery();
    }

    private void StartTestCaseDiscovery()
    {
        var taskStartParams = new TaskStartParams
        {
            TaskId = _taskId,
            OriginId = _originId,
            Message = "",
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
            Console.WriteLine("Discovery: " + discoveredTestCases.FirstOrDefault()?.DisplayName);

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
                    Source = testCase.Source,
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
            Message = "",
            EventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = StatusCode.Ok,
            DataKind = TaskFinishDataKind.TestCaseDiscoveryFinish,
        };
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildTaskFinish, taskFinishParams, CancellationToken.None);
        Console.WriteLine("DiscoveryComplete");
        waitHandle.Set();
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        Console.WriteLine("Discovery Message: " + message);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }
}
