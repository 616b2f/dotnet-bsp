namespace dotnet_bsp;

public record LaunchSettingsProfile(
    string CommandName,
    string? CommandLineArgs,
    bool DotnetRunMessages,
    bool LaunchBrowser,
    string LaunchUrl,
    string ApplicationUrl,
    Dictionary<string, string> EnvironmentVariables
);