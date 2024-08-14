using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace dotnet_bsp.Handlers;

public class RunEventHandler : ITestRunEventsHandler
{
    private AutoResetEvent waitHandle;

    public List<MsTestResult> TestResults { get; private set; }

    public RunEventHandler(AutoResetEvent waitHandle)
    {
        this.waitHandle = waitHandle;
        this.TestResults = new List<MsTestResult>();
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        Console.WriteLine("Run Message: " + message);
    }

    public void HandleTestRunComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {
        if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
        {
            this.TestResults.AddRange(lastChunkArgs.NewTestResults);
        }

        Console.WriteLine("TestRunComplete");
        waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
    {
        if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
        {
            this.TestResults.AddRange(testRunChangedArgs.NewTestResults);
        }
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
