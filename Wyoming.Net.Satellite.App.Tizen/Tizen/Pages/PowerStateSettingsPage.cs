using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Tizen.TV.System.Sensor;
using Wyoming.Net.Satellite.App.Tz.ViewModels;
using Wyoming.Net.Satellite.App.Tz.Components;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class PowerStateSettingsPage : ContentPage, ISelectableView
{
    public PowerStateSettingsPage(PowerStateSettingsViewModel vm, View parent)
    {
        var scrollable = new ScrollableBase
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            ScrollingDirection = ScrollableBase.Direction.Vertical,
            Padding = new Extents(200, 200, 20, 20),
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };

        var description = new TextLabel("When enabled, the satellite will automatically start when motion is detected near the TV and stop after a period of no motion. This helps save resources when no one is watching.")
        {
            PointSize = 22,
            TextColor = new Color("#9CA3AF"),
            Margin = new Extents(0, 0, 0, 30),
            Focusable = false,
            MultiLine = true,
            WidthResizePolicy = ResizePolicyType.FillToParent,
        };
        scrollable.Add(description);

        if (!MotionSensor.IsSupported)
        {
            var unsupportedLabel = new TextLabel("Motion sensor is not available on this device.")
            {
                PointSize = 26,
                TextColor = new Color("#DC2626"),
                Margin = new Extents(0, 0, 20, 0),
                Focusable = false,
                MultiLine = true,
                WidthResizePolicy = ResizePolicyType.FillToParent,
            };
            scrollable.Add(unsupportedLabel);
            Content = scrollable;
            Focusable = true;
            return;
        }

        var enabledLabel = TizenUI.CreateLabel("Motion Sensor Enabled");
        var enabledInput = TizenUI.CreateToggle(vm, (it) => it.MotionSensorEnabled, (it, value) => it.MotionSensorEnabled = value);
        enabledInput.UpFocusableView = parent;

        var turnOffScreenLabel = TizenUI.CreateLabel("Turn Off Screen on No Motion");
        var turnOffScreenInput = TizenUI.CreateToggle(vm, (it) => it.TurnOffScreen, (it, value) => it.TurnOffScreen = value);

        var timeoutLabel = TizenUI.CreateLabel("No Motion Timeout (seconds)");
        var timeoutInput = TizenUI.CreateInput(vm, (it) => it.NoMotionTimeoutSeconds, (it, value) => it.NoMotionTimeoutSeconds = value.ToIntOrDefault(), isLastField: true);

        enabledInput.DownFocusableView = turnOffScreenInput;
        turnOffScreenInput.UpFocusableView = enabledInput;
        turnOffScreenInput.DownFocusableView = timeoutInput;
        timeoutInput.UpFocusableView = turnOffScreenInput;

        scrollable.Add(enabledLabel);
        scrollable.Add(enabledInput);
        scrollable.Add(turnOffScreenLabel);
        scrollable.Add(turnOffScreenInput);
        scrollable.Add(timeoutLabel);
        scrollable.Add(timeoutInput);

        Content = scrollable;
        Focusable = true;
        FocusGained += (s, args) => FocusManager.Instance.SetCurrentFocusView(enabledInput);
    }

    public bool Selected { get; set; }

    public View View => this;
}
