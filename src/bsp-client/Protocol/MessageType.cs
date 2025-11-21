namespace bsp4csharp.Protocol;

public enum MessageType
{
    /// <summary>
    /// Error message.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Info message.
    /// </summary>
    Info = 3,

    /// <summary>
    /// Log message.
    /// </summary>
    Log = 4,
}