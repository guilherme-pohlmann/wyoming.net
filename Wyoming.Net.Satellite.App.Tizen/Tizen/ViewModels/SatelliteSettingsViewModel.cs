namespace Wyoming.Net.Satellite.App.Tz.ViewModels;

using System.IO;
using System.Text.Json;
using Wyoming.Net.Satellite.App.Tz.Platform;

public sealed class SatelliteSettingsViewModel
{
    public WakeSettingsViewModel WakeSettings { get; set; } = new();
    
    public string? Area { get; set; } = "Sala";

    public string? Name { get; set; } = "The Frame";

    public string? Description { get;set; }

    public int Port { get; set; } = 10568;

    public bool IsValid(out string? message)
    {
        message = null;

        if (string.IsNullOrEmpty(Area))
        {
            message = "Please enter Area";
            return false;
        }

        if (string.IsNullOrEmpty(Name))
        {
            message = "Please enter Name";
            return false;
        }

        if(Port < 0 || Port > 65535)
        {
            message = "Port number is invalid";
            return false;
        }

        if(WakeSettings.Enabled)
        {
            return WakeSettings.IsValid(out message);
        }

        return true;
    }

    public void Save()
    {  
        File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(this));
    }

    public SatelliteSettings ToSatelliteSettings()
    {
        var settings = new SatelliteSettings()
        {
            Wake = WakeSettings.ToSatelliteSettings()
        };

        return settings;
    }

    public static SatelliteSettingsViewModel Load()
    {
        var file = GetSettingsFilePath();

        try
        {
            if (File.Exists(file))
            {
                return JsonSerializer.Deserialize<SatelliteSettingsViewModel>(File.ReadAllText(file)) ?? new();
            }

            return new();
        }
        catch 
        {
            return new();
        }
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(TizenAssetReader.DataDir, "settings.json");
    }
}

