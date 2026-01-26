using Wyoming.Net.Satellite.App.Maui;

namespace Wyoming.Net.Satellite.App.MacCatalyst;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();

        return builder.Build();
    }
}