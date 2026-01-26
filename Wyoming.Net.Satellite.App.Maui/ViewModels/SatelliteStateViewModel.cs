using CommunityToolkit.Mvvm.ComponentModel;

namespace Wyoming.Net.Satellite.App.Maui.ViewModels;

internal partial class SatelliteStateViewModel : ObservableObject
{
    [ObservableProperty]
    bool isRunning;

    [ObservableProperty]
    bool isPaused;

    [ObservableProperty]
    bool isStreaming;

    [ObservableProperty]
    bool serverConnected;

    [ObservableProperty]
    bool micMuted;
}
