using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.ViewModels;
using Wyoming.Net.Satellite.App.Tz.Components;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class VadSettingsPage : ContentPage, ISelectableView
{
    public VadSettingsPage(VadSettingsViewModel vm, View parent)
    {
        var enabledLabel = TizenUI.CreateLabel("Enabled");
        var enabledInput = TizenUI.CreateToggle(vm, (it) => it.Enabled, (it, value) => it.Enabled = value);

        var typeLabel = TizenUI.CreateLabel("Type (0=WebRtc)");
        var typeInput = TizenUI.CreateInput(vm, (it) => it.Type, (it, value) => it.Type = value.ToIntOrDefault());

        var webRtcModeLabel = TizenUI.CreateLabel("WebRtc Mode (0-3)");
        var webRtcModeInput = TizenUI.CreateInput(vm, (it) => it.WebRtcMode, (it, value) => it.WebRtcMode = value.ToIntOrDefault());

        var useEnergyGateLabel = TizenUI.CreateLabel("Use Energy Gate");
        var useEnergyGateInput = TizenUI.CreateToggle(vm, (it) => it.UseEnergyGate, (it, value) => it.UseEnergyGate = value);

        var energyGateThresholdLabel = TizenUI.CreateLabel("Energy Gate Threshold");
        var energyGateThresholdInput = TizenUI.CreateInput(vm, (it) => it.EnergyGateThreshold, (it, value) => it.EnergyGateThreshold = value.ToFloatOrDefault());

        enabledInput.UpFocusableView = parent;
        enabledInput.DownFocusableView = typeInput;

        typeInput.UpFocusableView = enabledInput;
        typeInput.DownFocusableView = webRtcModeInput;

        webRtcModeInput.UpFocusableView = typeInput;
        webRtcModeInput.DownFocusableView = useEnergyGateInput;

        useEnergyGateInput.UpFocusableView = webRtcModeInput;
        useEnergyGateInput.DownFocusableView = energyGateThresholdInput;

        energyGateThresholdInput.UpFocusableView = useEnergyGateInput;

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

        view.Add(enabledLabel);
        view.Add(enabledInput);
        view.Add(typeLabel);
        view.Add(typeInput);
        view.Add(webRtcModeLabel);
        view.Add(webRtcModeInput);
        view.Add(useEnergyGateLabel);
        view.Add(useEnergyGateInput);
        view.Add(energyGateThresholdLabel);
        view.Add(energyGateThresholdInput);
        Content = view;
        Focusable = true;
		FocusGained += (s,args) => FocusManager.Instance.SetCurrentFocusView(enabledInput);
    }

    public bool Selected { get; set; }

    public View View => this;
}
