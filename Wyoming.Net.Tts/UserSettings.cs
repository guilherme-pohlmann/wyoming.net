namespace Wyoming.Net.Tts;

public static class UserSettings
{
    public static string Model { get; set; } = string.Empty;
    
    public static bool UseCuda { get; set; }

    public static string DefaultVoice { get; set; } = string.Empty;
}