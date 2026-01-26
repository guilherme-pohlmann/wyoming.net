using Microsoft.Extensions.Logging;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Satellite.App.Maui.Abstractions;
using Wyoming.Net.Satellite.App.Maui.ViewModels;

namespace Wyoming.Net.Satellite.App.Maui
{
    public static class MauiProgramExtensions
    {
        public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
        {
            builder
                .UseMauiApp<MainApp>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
            //builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

            builder.Services.AddSingleton(_ => SatelliteSettingsViewModel.Load());

            builder.Services.AddSingleton<IAssetReader, AssetReader>();

            builder.AddPlatformProviders();

            return builder;
        }

        private static void AddPlatformProviders(this MauiAppBuilder builder)
        {
#if ANDROID
            builder.Services.AddSingleton<IMicInputProvider, DroidMicProvider>();
            builder.Services.AddSingleton<ISpeakerProvider, DroidSpeakerProvider>();
#endif
            
#if IOS
            builder.Services.AddSingleton<iOSSoundProvider>();
            builder.Services.AddSingleton<ISpeakerProvider>(f => f.GetService<iOSSoundProvider>()!);
            builder.Services.AddSingleton<IMicInputProvider>(f => f.GetService<iOSSoundProvider>()!);
#endif
            
#if MACCATALYST
            builder.Services.AddSingleton<MacSoundProvider>();
            builder.Services.AddSingleton<ISpeakerProvider>(f => f.GetService<MacSoundProvider>()!);
            builder.Services.AddSingleton<IMicInputProvider>(f => f.GetService<MacSoundProvider>()!);
#endif
        }
    }
}
