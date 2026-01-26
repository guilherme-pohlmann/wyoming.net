namespace Wyoming.Net.Satellite.App.Maui;

public partial class ListeningAnimation : ContentView
{
    public ListeningAnimation()
    {
        InitializeComponent();
    }
    
    private static readonly BindableProperty IsListeningProperty =
        BindableProperty.Create(
            nameof(IsListening),
            typeof(bool),
            typeof(ListeningAnimation),
            false,
            propertyChanged: OnListeningChanged);
    
    private static readonly BindableProperty IsConnectingProperty =
        BindableProperty.Create(
            nameof(IsConnecting),
            typeof(bool),
            typeof(ListeningAnimation),
            false,
            propertyChanged: OnConnectingChanged);
    
    private static readonly BindableProperty IsConnectedProperty =
        BindableProperty.Create(
            nameof(IsConnected),
            typeof(bool),
            typeof(ListeningAnimation),
            false,
            propertyChanged: OnConnectedChanged);

    public bool IsListening
    {
        get => (bool)GetValue(IsListeningProperty);
        set => SetValue(IsListeningProperty, value);
    }
    
    public bool IsConnecting
    {
        get => (bool)GetValue(IsConnectingProperty);
        set => SetValue(IsConnectingProperty, value);
    }
    
    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    private static void OnListeningChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ListeningAnimation)bindable;
        
        if (newValue is bool isListening and true)
        {
            view.StartAnimations(isListening);
        }
        else
        {
            view.StopAllAnimations();

            if (view.IsConnected)
            {
                OnConnectedChanged(bindable, true, true);
            }
        }
    }
    
    private static void OnConnectingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ListeningAnimation)bindable;

        bool isConnecting = (bool)newValue;
        
        view.ServerIndicator.IsVisible = isConnecting;
        view.ServerIndicator.IsRunning = isConnecting;
        view.StatusSubtitle.Text = isConnecting ? "Waiting for server..." : string.Empty;
    }
    
    private static void OnConnectedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ListeningAnimation)bindable;

        var connected =  (bool)newValue;

        if (connected)
        {
            view.ServerIndicator.IsVisible = false;
            view.ServerIndicator.IsRunning = false;
            view.StatusSubtitle.Text = "Waiting for wake word...";
            view.StatusTitle.Text = "Satellite connected";
        }
        else
        {
            view.ServerIndicator.IsVisible = false;
            view.ServerIndicator.IsRunning = false;
            view.StatusSubtitle.Text = "Press start to begin";
            view.StatusTitle.Text = "Satellite stopped";
        }
    }

    private void StopAllAnimations()
    {
        OuterPulse.AbortAnimation("pulse");
        MiddlePulse.AbortAnimation("pulse");
        
        OuterPulse.Scale = 1;
        OuterPulse.Opacity = 0;

        MiddlePulse.Scale = 1;
        MiddlePulse.Opacity = 0;
    }

    private void StartAnimations(bool listening)
    {
        StatusTitle.Text = listening ? "Listening..." : "Not Listening";
        StatusSubtitle.Text = listening
            ? "Speak your command"
            : "Press start to begin";

        MicCore.BackgroundColor = listening
            ? Color.FromArgb("#2563EB")
            : Colors.Gray;

        if (listening)
        {
            StartPulse(
                OuterPulse,
                minScale: 0.8,
                maxScale: 1.2,
                minOpacity: 0.2,
                maxOpacity: 0.6,
                duration: 2000);

            StartPulse(
                MiddlePulse,
                minScale: 0.8,
                maxScale: 1.1,
                minOpacity: 0.3,
                maxOpacity: 0.7,
                duration: 2000);
        }
        else
        {
            StopAllAnimations();
        }
    }

    private static void StartPulse(
        VisualElement element,
        double minScale,
        double maxScale,
        double minOpacity,
        double maxOpacity,
        uint duration)
    {
        var animation = new Animation();

        // SCALE OUT
        animation.Add(0.0, 0.5,
            new Animation(v => element.Scale = v,
                minScale, maxScale, Easing.SinInOut));

        // SCALE IN
        animation.Add(0.5, 1.0,
            new Animation(v => element.Scale = v,
                maxScale, minScale, Easing.SinInOut));

        // OPACITY OUT
        animation.Add(0.0, 0.5,
            new Animation(v => element.Opacity = v,
                maxOpacity, minOpacity, Easing.SinInOut));

        // OPACITY IN
        animation.Add(0.5, 1.0,
            new Animation(v => element.Opacity = v,
                minOpacity, maxOpacity, Easing.SinInOut));

        animation.Commit(
            element,
            "pulse",
            length: duration,
            easing: Easing.Linear,
            repeat: () => true);
    }
}