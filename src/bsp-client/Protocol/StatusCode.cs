namespace bsp4csharp.Protocol;

public enum StatusCode
{
  /** Execution was successful. */
  Ok = 1,

  /** Execution failed. */
  Error = 2,

  /** Execution was cancelled. */
  Cancelled = 3,
}

