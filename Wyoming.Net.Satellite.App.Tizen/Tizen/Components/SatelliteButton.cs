using Tizen.NUI;
using Tizen.NUI.Components;

namespace Wyoming.Net.Satellite.App.Tz.Components;

internal sealed class SatelliteButton : Button
{
    enum SatelliteButtonState
    {
        Paused,
        Started
    }

    private SatelliteButtonState _state;

    public SatelliteButton()
    {
        Text = "Start Satellite";
        Focusable = true;
        FocusNavigationSupport = true;
        BorderlineColor = TvStyle.ButtonBorderlineColor;
        BorderlineWidth = 2;
        BackgroundColor = TvStyle.ButtonBackgroundColor;
        TextColor = Color.White;
        Margin = new Extents(0, 0, 60, 0);
        Padding = new Extents(20, 20, 20, 20);
        CellVerticalAlignment = VerticalAlignmentType.Center;
        _state = SatelliteButtonState.Paused;
    }

    public override void OnFocusGained()
    {
        Scale = new Vector3(1.12f, 1.12f, 1);

        if (_state != SatelliteButtonState.Started)
        {
            BackgroundColor = TvStyle.ButtonFocusedBackgroundColor;
            BorderlineColor = TvStyle.ButtonFocusedBorderlineColor;
        }

        base.OnFocusGained();
    }

    public override void OnFocusLost()
    {

        Scale = Vector3.One;

        if (_state != SatelliteButtonState.Started)
        {
            BackgroundColor = TvStyle.ButtonBackgroundColor;
            BorderlineColor = TvStyle.ButtonBorderlineColor;
        }

        base.OnFocusLost();
    }

    public void FlipState()
    {
        if (_state == SatelliteButtonState.Started)
        {
            _state = SatelliteButtonState.Paused;

            Text = "Start Satellite";
            BackgroundColor = TvStyle.ButtonBackgroundColor;
        }
        else
        {
            _state = SatelliteButtonState.Started;

            Text = "Stop Satellite";
            BackgroundColor = Color.Red;
        }
    }
}
