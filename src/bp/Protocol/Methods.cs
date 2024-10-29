namespace BaseProtocol.Protocol;

/// <summary>
/// Class which contains the string values for all common build server protocol methods.
/// </summary>
public static class Methods
{
  // client requests
  public const string Initialize = "initialize";
  public const string Initialized = "initialized";
  public const string ClientRegisterCapability = "client/registerCapability";
  public const string ClientUnregisterCapability = "client/unregisterCapability";
  public const string SetTrace = "$/setTrace";
  public const string LogTrace = "$/logTrace";
  public const string Shutdown = "shutdown";

  // client notification requests
  public const string CancelRequest = "$/cancelRequest";

  // sever requests
  public const string WindowShowMessageRequest = "window/showMessageRequest";

  // server notifications
  public const string Exit = "exit";
  public const string WindowShowMessage = "window/showMessage";
  public const string WindowLogMessage = "window/logMessage";
  public const string TelemetryEvent = "telemetry/event";
  public const string Progress = "$/progress";
}