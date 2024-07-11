using Newtonsoft.Json;

namespace dotnet_bsp;

public record LaunchSettings(IDictionary<string, LaunchSettingsProfile> Profiles)
{
    public static bool TryLoadLaunchSettings(string launchSettingsPath, out LaunchSettings? launchSettings)
    {
        if (File.Exists(launchSettingsPath))
        {
            var content = File.ReadAllText(launchSettingsPath);
            launchSettings = JsonConvert.DeserializeObject<LaunchSettings>(content);
            if (launchSettings is not null)
            {
                return true;
            }
        }

        launchSettings = null!;
        return false;
    }
}