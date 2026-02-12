using System;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;
using Wyoming.Net.Satellite.App.Tz.Components;
using Wyoming.Net.Satellite.App.Tz.Platform;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class MainPage : View
{
    private ListeningAnimationComponent listeningAnimationComponent;

    private SatelliteStateViewModel stateViewModel;

    private WakeWordSatellite? satellite;

    private AsyncTcpServer? server;

    private TizenSpeakerProvider speakerProvider;

    private SatelliteButton startStopButton;

    private readonly View parent;
    private readonly SynchronizationContext uiContext;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MainPage(View parent)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        this.parent = parent;
        uiContext = TizenSynchronizationContext.Current!;

        InitializeUI();
        InitializeViewModel();
        speakerProvider = new TizenSpeakerProvider();
    }

    private void InitializeUI()
    {
        Focusable = true;
        FocusGained += OnFocus;

        var view = new View
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            Padding = new Extents(0, 0, 70, 0),
            Layout = new LinearLayout()
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };

        var title = TizenUI.CreateLabel("Wyoming .NET");
        title.PointSize = 40;
        title.Padding = new Extents(0, 0, 40, 40);
        title.TextColor = Color.White;

        listeningAnimationComponent = new ListeningAnimationComponent()
        {
            Margin = new Extents(0, 0, 40, 60)
        };


        startStopButton = new SatelliteButton
        {
            UpFocusableView = parent,
        };
        startStopButton.Clicked += async (s, args) => await ToggleServer();

        view.Add(title);
        view.Add(listeningAnimationComponent);
        view.Add(startStopButton);

        Add(view);
    }

    private void OnFocus(object? sender, EventArgs args)
    {
        FocusManager.Instance.SetCurrentFocusView(startStopButton);
    }

    private void InitializeViewModel()
    {
        stateViewModel = new SatelliteStateViewModel();
    }

    private async Task<bool> CreateServerAsync()
    {
        var settingsViewModel = SatelliteSettingsViewModel.Load();

        if (!settingsViewModel.IsValid(out var message))
        {
            TvDialog.ShowOkDialog("Ops", message!);
            return false;
        }
        
        var settings = settingsViewModel.ToSatelliteSettings();
        var wakeModels = await settingsViewModel.WakeSettings.GetModelsAsync();
        var loggerFactory = new TizenLoggerFactory();

        satellite = new WakeWordSatellite(settings, wakeModels, loggerFactory, new TizenMicProvider(), speakerProvider);
        satellite.StateChanged += OnSatelliteStateChanged;
        satellite.SatelliteError += OnSatelliteError;
        satellite.WakeWordDetected += OnWakeWordDetected;

        var info = new Info(new Core.Events.Satellite()
        {
            ActiveWakeWords = new string[] { settings.Wake.Name! },
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

    private async Task StartServerAsync()
    {
        if (server is null && !await CreateServerAsync())
        {
            return;
        }

        listeningAnimationComponent.IsConnecting = true;
        stateViewModel.IsRunning = true;

        await server!.StartAsync();
        startStopButton.FlipState();
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
            listeningAnimationComponent.IsConnecting = false;
            listeningAnimationComponent.IsConnected = stateViewModel.ServerConnected;
            listeningAnimationComponent.IsListening = stateViewModel.IsStreaming;
        });
    }

    private async Task OnWakeWordDetected()
    {
        var wav = await TizenAssetReader.ReadAssetAsync("ww_detected3.wav");
        var wavInfo = WavHelper.ReadWavInfo(wav);

        await speakerProvider.StartAsync(wavInfo.SampleRate, wavInfo.BytesPerSample, wavInfo.Channels);
        await speakerProvider.PlayAsync(wav, null);
        await speakerProvider.StopAsync();
    }

    private async Task OnSatelliteError(Exception exception)
    {
        await StopServerAsync();

        RunUIUpdate(async () =>
        {

        });
    }

    private async Task StopServerAsync()
    {
        if (server is not null)
        {
            await server.StopAsync();
            satellite = null;
            server = null;
            stateViewModel.IsRunning = false;
            listeningAnimationComponent.IsConnected = false;
            listeningAnimationComponent.IsConnecting = false;

            startStopButton.FlipState();
        }
    }

    private void RunUIUpdate(Action action)
    {
        uiContext.Post((_) => action(), null);
    }
}