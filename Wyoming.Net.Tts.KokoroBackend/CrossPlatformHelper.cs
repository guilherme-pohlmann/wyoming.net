using System.Runtime.InteropServices;

namespace Wyoming.Net.Tts.KokoroBackend;

internal static class CrossPlatformHelper 
{
    public static string GetModelPath(string model)
    {
        return string.Intern(Path.Combine(GetResourcesBasePath(), "model", model));
    }
    
    public static string GetVoicesPath()
    {
        return Path.Combine(GetResourcesBasePath(), "voices");
    }
    
    public static string GetEspeakBasePath() 
    {
        return Path.Combine(GetResourcesBasePath(), "espeak");
    }
    
    public static string GetEspeakBinariesPath() 
    {
        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64.dll" : "amd64.dll";
        return Path.Combine(GetEspeakBasePath(), $"espeak-ng-{GetOsSuffix()}{arch}");
    }

    private static string GetOsSuffix()
    {
        if (OperatingSystem.IsWindows())
        {
            return "win-";
        }
        
        if (OperatingSystem.IsLinux())
        {
            return "linux-";
        } 
        
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return "macos-";
        }

        throw new PlatformNotSupportedException();
    }
    
    private static string GetResourcesBasePath()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
