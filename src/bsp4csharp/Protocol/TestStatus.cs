namespace bsp4csharp.Protocol;

public enum TestStatus
{
    /** The test passed successfully. */
    Passed = 1,

    /** The test failed. */
    Failed = 2,

    /** The test was marked as ignored. */
    Ignored = 3,

    /** The test execution was cancelled. */
    Cancelled = 4,

    /** The was not included in execution. */
    Skipped = 5,
}