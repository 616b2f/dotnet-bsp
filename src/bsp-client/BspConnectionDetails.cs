namespace bsp_client;

public record BspConnectionDetails
{
    /** The name of the build tool. */
    public required string Name { get; init; }
    /** The version of the build tool. */
    public required string Version { get; init; }
    /** The bsp version of the build tool. */
    public required string BspVersion { get; init; }
    /** A collection of languages supported by this BSP server. */
    public required string[] Languages { get; init; }
    /** Command arguments runnable via system processes to start a BSP server */
    public required string[] Argv { get; init; }
}