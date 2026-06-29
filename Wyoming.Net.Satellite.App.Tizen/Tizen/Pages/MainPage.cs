using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Wyoming.Net.Satellite.App.Tz.Components;
using Wyoming.Net.Satellite.App.Tz.Platform;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class MainPage : View, ISelectableView
{
    private ListeningAnimationComponent listeningAnimationComponent;

    private SatelliteStateViewModel stateViewModel = new();

    private SatelliteButton startStopButton;

    private readonly View parent;

    private readonly SynchronizationContext uiContext;

    private bool selected;

    private System.Threading.Timer statusTimer;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MainPage(View parent)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        this.parent = parent;
        uiContext = TizenSynchronizationContext.Current!;
       

        InitializeUI();
        ServiceManager.Singleton.MessageReceived += (s, e) =>
        {
            string eventName = e.Message.GetItem<string>(Constants.Events.EventKey);
            RunUIUpdate(() => HandleServiceEvent(eventName, e.Message));
        };
    }

    public bool Selected
    {
        get
        {
            return selected;
        }
        set
        {
            selected = value;

            if (selected)
            {
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }
    }

    public View View => this;

    private void StartTimer()
    {
        statusTimer = new System.Threading.Timer(_ => ServiceManager.Singleton.SendGetStatus(), null, 1000, Timeout.Infinite);
    }

    private void StopTimer()
    {
        statusTimer?.Dispose();
        statusTimer = null;
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
        startStopButton.Clicked += (s, args) => ToggleServer();

        view.Add(title);
        view.Add(listeningAnimationComponent);
        view.Add(startStopButton);

        Add(view);
    }

    private void OnFocus(object? sender, EventArgs args)
    {
        FocusManager.Instance.SetCurrentFocusView(startStopButton);
    }

    private void HandleServiceEvent(string eventName, Bundle data)
    {
        if (eventName == Constants.Events.StateChangedEvent)
        {
            bool isConnecting = bool.Parse(data.GetItem<string>("isConnecting"));
            listeningAnimationComponent.IsConnecting = isConnecting;
            listeningAnimationComponent.IsConnected = bool.Parse(data.GetItem<string>("isConnected"));
            listeningAnimationComponent.IsListening = bool.Parse(data.GetItem<string>("isStreaming"));

            bool isRunning = bool.Parse(data.GetItem<string>("isRunning")) || isConnecting;

            if (isRunning != stateViewModel.IsRunning)
            {
                stateViewModel.IsRunning = isRunning;
                startStopButton.FlipState();
            }

            return;
        }

        if (eventName == Constants.Events.ErrorEvent)
        {
            OnSatelliteError(data.GetItem<string>("errorDetails"));
        }
    }

    private void ToggleServer()
    {
        if (stateViewModel.IsRunning)
        {
            ServiceManager.Singleton.SendStopSatellite();
        }
        else
        {
            ServiceManager.Singleton.SendStartSatellite();
        }
    }

    private void OnSatelliteError(string? details)
    {
        RunUIUpdate(async () =>
        {
            TvDialog.ShowOkDialog("Ops", $"Error from satellite: {details}");
        });
    }

    private void RunUIUpdate(Action action)
    {
        uiContext.Post((_) => action(), null);
    }
}