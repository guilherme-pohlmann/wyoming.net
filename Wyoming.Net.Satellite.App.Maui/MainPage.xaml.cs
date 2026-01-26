using Microsoft.Extensions.Logging;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;
using Wyoming.Net.Satellite.App.Maui.Abstractions;
using Wyoming.Net.Satellite.App.Maui.ViewModels;

namespace Wyoming.Net.Satellite.App.Maui;

public partial class MainPage : ContentPage
{
    private readonly ILoggerFactory loggerFactory;

    private readonly IMicInputProvider micProvider;
    private readonly ISpeakerProvider speakerProvider;
    private readonly SatelliteSettingsViewModel settingsViewModel;
    private readonly SatelliteStateViewModel stateViewModel;
    private readonly IAssetReader assetReader;
    private WakeWordSatellite? satellite;
    private AsyncTcpServer? server;

    public MainPage(
        ILoggerFactory loggerFactory, 
        IMicInputProvider micInputProvider,
        ISpeakerProvider speakerProvider, 
        IAssetReader assetReader,
        SatelliteSettingsViewModel vm
        )
    {
        InitializeComponent();
        this.loggerFactory = loggerFactory;
        this.micProvider = micInputProvider;
        this.speakerProvider = speakerProvider;
        this.assetReader = assetReader;
        this.settingsViewModel = vm;
        this.stateViewModel = new SatelliteStateViewModel();

        BindingContext = this.stateViewModel;
    }
    
    private async Task<bool> CreateServerAsync()
    {
        if (!settingsViewModel.IsValid(out var message))
        {
            await DisplayAlert(
                "Failed start satellite",
                message,
                "OK");

            return false;
        }

        var settings = settingsViewModel.ToSatelliteSettings();
        var wakeModels = await settingsViewModel.WakeSettings.GetModelsAsync(assetReader);

        satellite = new WakeWordSatellite(settings, wakeModels, loggerFactory, micProvider, speakerProvider);
        satellite.StateChanged += OnSatelliteStateChanged;
        satellite.SatelliteError += OnSatelliteError;
        satellite.WakeWordDetected  += OnWakeWordDetected;

        var info = new Info(new Core.Events.Satellite()
        {
            ActiveWakeWords = [settings.Wake.Name!],
            Attribution = new Attribution
            {
                Name = "Guilherme Pohlmann da Rosa",
                Url = "https://github.com/guilherme-pohlmann/wyoming-net"
            },
            Description = settingsViewModel.Description,
            Name = settingsViewModel.Name!,
            HasVad = false,
            Installed = true,
            MaxActiveWakeWords = 1,
            SupportsTrigger = true,
            Version = "0.0.1",
            Area = settingsViewModel.Area,
        });

        server = new AsyncTcpServer(
           "0.0.0.0",
           settingsViewModel.Port,
           (client, server, loggerFactory) => new SatelliteEventHandler(client, server, loggerFactory, satellite, info),
           loggerFactory);

        return true;
    }

    private void OnSatelliteStateChanged()
    {
        Asserts.IsNotNull(satellite);

        stateViewModel.IsStreaming = satellite!.IsStreaming;
        stateViewModel.IsRunning = satellite.IsRunning;
        stateViewModel.IsPaused = satellite.IsPaused;
        stateViewModel.MicMuted = satellite.MicMuted;
        stateViewModel.ServerConnected = !string.IsNullOrEmpty(satellite.ServerId);

        RunUIUpdate(() =>
        {
            ListeningAnimation.IsConnecting = false;
            ListeningAnimation.IsConnected = stateViewModel.ServerConnected;
            ListeningAnimation.IsListening = stateViewModel.IsStreaming; 
        });
    }

    private async Task OnSatelliteError(Exception exception)
    {
        await StopServerAsync();
        await RunUIUpdateAsync(async () =>
        {
            await DisplayAlert(
                "Satellite Error",
                exception.Message,
                "OK");
        });
    }

    private async Task OnWakeWordDetected()
    {
        var wav = await assetReader.ReadBytesAsync("ww_detected3.wav");
        var wavInfo = WavHelper.ReadWavInfo(wav);

        await speakerProvider.StartAsync(wavInfo.SampleRate, wavInfo.BytesPerSample, wavInfo.Channels);
        await speakerProvider.PlayAsync(wav, null);
        await speakerProvider.StopAsync();
    }

    private async void OnStartStopClicked(object sender, EventArgs args)
    {
        await ToggleServer();
    }

    private async Task ToggleServer()
    {
        if (stateViewModel.IsRunning)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    private async Task StartServerAsync()
    {
        if (server is null && !await CreateServerAsync())
        {
            return;
        }

        if(!await EnsureMicrophonePermissionAsync())
        {
            return;
        }
        
        ListeningAnimation.IsConnecting = true;
        
        await server!.StartAsync();
        
        RunUIUpdate(() =>
        {
            StartStopButton.Text = "Stop Satellite";
            StartStopButton.Background = new SolidColorBrush(Colors.Red);
        });
    }

    private async Task StopServerAsync()
    {
        if (server is not null)
        {
            await server.StopAsync();
            satellite = null;
            server = null;
            
            RunUIUpdate(() =>
            {
                StartStopButton.Text = "Start Satellite";
                StartStopButton.Background = new SolidColorBrush(Color.FromArgb("#4F46E5"));
            });
        }
    }

    private async Task<bool> EnsureMicrophonePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status == PermissionStatus.Granted)
            return true;

        if (Permissions.ShouldShowRationale<Permissions.Microphone>())
        {
            await DisplayAlert(
                "Microphone permission",
                "This app needs access to the microphone to record audio.",
                "OK");
        }

        status = await Permissions.RequestAsync<Permissions.Microphone>();

        return status == PermissionStatus.Granted;
    }
    
    private static void RunUIUpdate(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }
    
    private static Task RunUIUpdateAsync(Func<Task> action)
    {
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
