using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace test;

internal class XunitTraceListener : TraceListener
{
    private readonly ITestOutputHelper logger;
    private readonly StringBuilder lineInProgress = new();
    private bool disposed;

    internal XunitTraceListener(ITestOutputHelper logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool IsThreadSafe => false;

    public override void Write(string? message) => lineInProgress.Append(message);

    public override void WriteLine(string? message)
    {
        if (!disposed)
        {
            lineInProgress.Append(message);
            logger.WriteLine(lineInProgress.ToString());
            lineInProgress.Clear();
        }
    }

    protected override void Dispose(bool disposing)
    {
        disposed = true;
        base.Dispose(disposing);
    }
}