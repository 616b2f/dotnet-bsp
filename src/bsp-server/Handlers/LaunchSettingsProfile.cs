namespace dotnet_bsp.Handlers;

public record LaunchSettingsProfile(
    string CommandName,
    bool DotnetRunMessages,
    bool LaunchBrowser,
    string LaunchUrl,
    string ApplicationUrl,
    Dictionary<string, string> EnvironmentVariables
);