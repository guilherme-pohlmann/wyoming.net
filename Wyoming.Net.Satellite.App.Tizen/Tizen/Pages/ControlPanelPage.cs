using System;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.Components;
using Wyoming.Net.Satellite.App.Tz.Platform;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class ControlPanelPage : ContentPage, ISelectableView
{
    private readonly ControlPanelViewModel _vm;
    private readonly TextLabel _statusLabel;
    private readonly SatelliteButton _startStopButton;

    private Timer _statusTimer;

    public ControlPanelPage(ControlPanelViewModel vm, View parent)
    {
        _vm = vm;

        _statusLabel = TizenUI.CreateLabel("Service Status: Checking...");
        _statusLabel.PointSize = 32;
        _statusLabel.Margin = new Extents(0, 0, 0, 40);

        _startStopButton = new SatelliteButton("Start background service", "Kill background service")
        {
            UpFocusableView = parent,
            Margin = new Extents(0, 0, 0, 40)
        };
        _startStopButton.Clicked += (s, args) => ToggleService();

        var ipLabel = TizenUI.CreateLabel("Remote Log IP");
        var ipInput = TizenUI.CreateInput(vm, it => it.RemoteLogIp, (it, value) => it.RemoteLogIp = value);

        var portLabel = TizenUI.CreateLabel("Remote Log Port");
        var portInput = TizenUI.CreateInput(vm, it => it.RemoteLogPort, (it, value) => it.RemoteLogPort = value.ToIntOrDefault());

        var debugAudioLabel = TizenUI.CreateLabel("Debug Audio");
        var debugAudioToggle = TizenUI.CreateToggle(vm, it => it.DebugAudioEnabled, (it, value) => it.DebugAudioEnabled = value);

        var debugFileServerLabel = TizenUI.CreateLabel("Debug File Server (port 8089)");
        var debugFileServerToggle = TizenUI.CreateToggle(vm, it => it.DebugFileServerEnabled, (it, value) => it.DebugFileServerEnabled = value);

        _startStopButton.DownFocusableView = debugAudioToggle;

        debugAudioToggle.UpFocusableView = _startStopButton;
        debugAudioToggle.DownFocusableView = debugFileServerToggle;

        debugFileServerToggle.UpFocusableView = debugAudioToggle;
        debugFileServerToggle.DownFocusableView = ipInput;

        ipInput.UpFocusableView = debugFileServerToggle;
        ipInput.DownFocusableView = portInput;

        portInput.UpFocusableView = ipInput;

        ipInput.FocusLost += (s, e) => RestartRemoteLogger();
        portInput.FocusLost += (s, e) => RestartRemoteLogger();

        var view = new View
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            Padding = new Extents(200, 200, 0, 0),
            Margin = new Extents(50, 50, 50, 50),
            Layout = new LinearLayout()
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };

        view.Add(_statusLabel);
        view.Add(_startStopButton);
        view.Add(debugAudioLabel);
        view.Add(debugAudioToggle);
        view.Add(debugFileServerLabel);
        view.Add(debugFileServerToggle);
        view.Add(ipLabel);
        view.Add(ipInput);
        view.Add(portLabel);
        view.Add(portInput);

        Content = view;
        Focusable = true;
        FocusGained += (s, args) =>
        {
            FocusManager.Instance.SetCurrentFocusView(_startStopButton);
        };

        parent.ChildAdded += (s, args) =>
        {
            if (args.Added == this)
            {
                UpdateServiceStatus();

                _statusTimer = new Timer(1500);
                _statusTimer.Tick += (s, e) =>
                {
                    UpdateServiceStatus();
                    return true;
                };
                _statusTimer.Start();
            }
        };

        parent.ChildRemoved += (s, args) =>
        {
            if (args.Removed == this)
            {
                _statusTimer?.Stop();
                _statusTimer?.Dispose();
            }
        };
    }

     public bool Selected { get; set; }

     public View View => this;

    private void UpdateServiceStatus()
    {
        var state = ApplicationHelper.CheckServiceState();
        bool running = state == ApplicationRunningContext.AppState.Service;

        if (running)
        {
            _statusLabel.Text = "Background service status: Running";
            _startStopButton.StartState();
        }
        else
        {
            _statusLabel.Text = "Background service status: Not Running";
            _startStopButton.StopState();
        }
    }

    private async void ToggleService()
    {
        var state = ApplicationHelper.CheckServiceState();
        bool running = state == ApplicationRunningContext.AppState.Service;

        if (running)
        {
            _statusTimer?.Stop();
            _statusLabel.Text = "Killing background service...";

            if (ServiceManager.Singleton.IsRunning && ServiceManager.Singleton.IsCommunicating)
            {
                ServiceManager.Singleton.SendStopSatellite();
            }
            await ServiceManager.Singleton.KillService();
            await Task.Delay(TimeSpan.FromSeconds(3));

            _statusTimer?.Start();
        }
        else
        {
            _statusTimer?.Stop();
            _statusLabel.Text = "Starting background service...";

            await ServiceManager.Singleton.StartAsync();
            await Task.Delay(TimeSpan.FromSeconds(3));

            _statusTimer?.Start();
        }
    }

    private void RestartRemoteLogger()
    {
        RemoteLogger.Restart(_vm.RemoteLogIp, _vm.RemoteLogPort);
    }
}
