using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace Wyoming.Net.Satellite.App.Maui.ViewModels;

public partial class SatelliteSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    WakeSettingsViewModel wakeSettings = new();

    [ObservableProperty]
    string? area;

    [ObservableProperty]
    string? name;

    [ObservableProperty]
    string? description;

    [ObservableProperty]
    int port = 10568;

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
        return Path.Combine(FileSystem.AppDataDirectory, "settings.json");
    }
}

