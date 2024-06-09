namespace dotnet_bsp.Handlers;

public record LaunchSettings(IDictionary<string, LaunchSettingsProfile> Profiles);