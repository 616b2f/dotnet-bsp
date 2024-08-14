using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace dotnet_bsp.Handlers;

public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    private AutoResetEvent waitHandle;

    public List<TestCase> DiscoveredTestCases { get; private set; }

    public DiscoveryEventHandler(AutoResetEvent waitHandle)
    {
        this.waitHandle = waitHandle;
        this.DiscoveredTestCases = new List<TestCase>();
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        Console.WriteLine("Discovery: " + discoveredTestCases.FirstOrDefault()?.DisplayName);

        if (discoveredTestCases != null)
        {
            this.DiscoveredTestCases.AddRange(discoveredTestCases);
        }
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            this.DiscoveredTestCases.AddRange(lastChunk);
        }

        Console.WriteLine("DiscoveryComplete");
        waitHandle.Set();
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        Console.WriteLine("Discovery Message: " + message);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }
}
