using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.Platform;

namespace Wyoming.Net.Satellite.App.Tz.Components;

public sealed class ListeningAnimationComponent : View
{
    private View _pulseContainer;
    private View _outerPulse;
    private View _middlePulse;
    private View _micCore;
    private ImageView _micIcon;
    private TextLabel _statusTitle;
    private TextLabel _statusSubtitle;
    private Loading _serverIndicator;
    private Animation _pulseAnimation;
    private bool _isListening;
    private bool _isConnecting;
    private bool _isConnected;

    public bool IsListening
    {
        get => _isListening;
        set
        {

            if (value != _isListening)
            {
                _isListening = value;
                OnListeningChanged(value);
            }
        }
    }


    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (value != _isConnecting)
            {
                _isConnecting = value;
                OnConnectingChanged(value);
            }
        }
    }


    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (value != _isConnected)
            {
                _isConnected = value;
                OnConnectedChanged(value);
            }
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ListeningAnimationComponent()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Root Layout (Vertical Stack)
        this.Layout = new LinearLayout()
        {
            LinearOrientation = LinearLayout.Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            CellPadding = new Size2D(0, 5)
        };
        this.WidthSpecification = LayoutParamPolicies.MatchParent;
        this.HeightSpecification = LayoutParamPolicies.WrapContent;

        // MIC + PULSES Container (Absolute Layout equivalent)
        _pulseContainer = new View()
        {
            Size = new Size(192, 192),
        };

        // OUTER PULSE
        _outerPulse = CreateCircle(192, new Color("#60A5FA"), 0.0f);

        // MIDDLE PULSE
        _middlePulse = CreateCircle(160, new Color("#3B82F6"), 0.0f);

        // CENTER MIC CORE
        _micCore = CreateCircle(96, new Color("#2563EB"), 1.0f);

        _micIcon = new ImageView()
        {
            ResourceUrl = TizenAssetReader.GetResourcePath("mic.svg"),
            Size = new Size(48, 48),
            ParentOrigin = Position.ParentOriginCenter,
            PivotPoint = Position.PivotPointCenter,
            PositionUsesPivotPoint = true
        };
        _micCore.Add(_micIcon);

        _pulseContainer.Add(_outerPulse);
        _pulseContainer.Add(_middlePulse);
        _pulseContainer.Add(_micCore);
        this.Add(_pulseContainer);

        // STATUS TEXTS
        var textStack = new View()
        {
            Layout = new LinearLayout()
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                CellPadding = new Size2D(0, 4)
            },
            WidthSpecification = LayoutParamPolicies.MatchParent
        };

        _statusTitle = new TextLabel()
        {
            Text = "Satellite stopped",
            PointSize = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.White
        };

        _statusSubtitle = new TextLabel()
        {
            Text = "Press start to begin",
            PointSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Gray
        };

        _serverIndicator = new Loading()
        {
            CellHorizontalAlignment = HorizontalAlignmentType.Left,
            CellVerticalAlignment = VerticalAlignmentType.Bottom
        };
        _serverIndicator.Hide();

        textStack.Add(_statusTitle);
        textStack.Add(_statusSubtitle);
        Add(textStack);
    }

    private View CreateCircle(float size, Color color, float opacity)
    {
        return new View()
        {
            Size = new Size(size, size),
            BackgroundColor = color,
            Opacity = opacity,
            CornerRadius = size / 2,
            ParentOrigin = Position.ParentOriginCenter,
            PivotPoint = Position.PivotPointCenter,
            PositionUsesPivotPoint = true
        };
    }

    private void OnListeningChanged(bool listening)
    {
        if (listening)
        {
            _statusTitle.Text = "Listening...";
            _statusSubtitle.Text = "Speak your command";
            _micCore.BackgroundColor = new Color("#2563EB");
            StartPulseAnimations();
        }
        else
        {
            StopPulseAnimations();

            if (IsConnected)
            {
                OnConnectedChanged(true);
            }
        }
    }

    private void OnConnectingChanged(bool connecting)
    {
        _statusSubtitle.Text = connecting ? "Waiting for server..." : "Press start to begin";
    }

    private void OnConnectedChanged(bool connected)
    {
        if (connected)
        {
            _statusTitle.Text = "Satellite connected";
            _statusSubtitle.Text = "Waiting for wake word...";
        }
        else
        {
            _statusTitle.Text = "Satellite stopped";
            _statusSubtitle.Text = "Press start to begin";
        }
    }

    private void StartPulseAnimations()
    {
        _pulseAnimation = new Animation(2000);

        // Outer Pulse: Scale and Opacity
        _pulseAnimation.AnimateTo(_outerPulse, "Scale", new Vector3(1.2f, 1.2f, 1.0f), 0, 1000, new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine));
        _pulseAnimation.AnimateTo(_outerPulse, "Scale", new Vector3(0.8f, 0.8f, 1.0f), 1000, 2000, new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine));
        _pulseAnimation.AnimateTo(_outerPulse, "Opacity", 0.6f, 0, 1000);
        _pulseAnimation.AnimateTo(_outerPulse, "Opacity", 0.2f, 1000, 2000);

        // Middle Pulse: Scale and Opacity
        _pulseAnimation.AnimateTo(_middlePulse, "Scale", new Vector3(1.1f, 1.1f, 1.0f), 0, 1000, new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine));
        _pulseAnimation.AnimateTo(_middlePulse, "Scale", new Vector3(0.8f, 0.8f, 1.0f), 1000, 2000, new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseInOutSine));
        _pulseAnimation.AnimateTo(_middlePulse, "Opacity", 0.7f, 0, 1000);
        _pulseAnimation.AnimateTo(_middlePulse, "Opacity", 0.3f, 1000, 2000);

        _pulseAnimation.Looping = true;
        _pulseAnimation.Play();
    }

    private void StopPulseAnimations()
    {
        if (_pulseAnimation is not null)
        {
            _pulseAnimation.EndAction = Animation.EndActions.StopFinal;
            _pulseAnimation.Stop();
            _pulseAnimation.Looping = false;
        }
        _outerPulse.Opacity = 0;
        _middlePulse.Opacity = 0;
        _outerPulse.Scale = Vector3.One;
        _middlePulse.Scale = Vector3.One;
    }
}