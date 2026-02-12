using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class WakeSettingsPage : ContentPage
{
    public WakeSettingsPage(WakeSettingsViewModel vm, View parent)
    {
        var modelLabel = TizenUI.CreateLabel("Model");
        var modelInput = TizenUI.CreateInput(vm, (it) => it.Model, (it, value) => it.Model = value);

        var rateLabel = TizenUI.CreateLabel("Rate");
        var rateInput = TizenUI.CreateInput(vm, (it) => it.Rate, (it, value) => it.Rate = value.ToIntOrDefault());

        var widthLabel = TizenUI.CreateLabel("Width");
        var widthInput = TizenUI.CreateInput(vm, (it) => it.Width, (it, value) => it.Width = value.ToIntOrDefault());

        var channelsLabel = TizenUI.CreateLabel("Channels");
        var channelsInput = TizenUI.CreateInput(vm, (it) => it.Channels, (it, value) => it.Channels = value.ToIntOrDefault());

        var refracLabel = TizenUI.CreateLabel("Refractory Seconds");
        var refracInput = TizenUI.CreateInput(vm, (it) => it.RefractorySeconds, (it, value) => it.RefractorySeconds = value.ToIntOrDefault());

        var patienceLabel = TizenUI.CreateLabel("Max Patience");
        var patienceInput = TizenUI.CreateInput(vm, (it) => it.MaxPatience, (it, value) => it.MaxPatience = value.ToIntOrDefault());

        var thresholdLabel = TizenUI.CreateLabel("Prediction Threshold");
        var thresholdInput = TizenUI.CreateInput(vm, (it) => it.PredictionThreshold, (it, value) => it.PredictionThreshold = value.ToFloatOrDefault());

        modelInput.UpFocusableView = parent;
        modelInput.DownFocusableView = rateInput;

        rateInput.UpFocusableView = modelInput;
        rateInput.DownFocusableView = widthInput;

        widthInput.UpFocusableView = rateInput;
        widthInput.DownFocusableView = channelsInput;

        channelsInput.UpFocusableView = widthInput;
        channelsInput.DownFocusableView = refracInput;

        refracInput.UpFocusableView = channelsInput;
        refracInput.DownFocusableView = patienceInput;

        patienceInput.UpFocusableView = refracInput;
        patienceInput.DownFocusableView = thresholdInput;

        thresholdInput.UpFocusableView = patienceInput;

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

        view.Add(modelLabel);
        view.Add(modelInput);
        view.Add(rateLabel);
        view.Add(rateInput);
        view.Add(widthLabel);
        view.Add(widthInput);
        view.Add(channelsLabel);
        view.Add(channelsInput);
        view.Add(refracLabel);
        view.Add(refracInput);
        view.Add(patienceLabel);
        view.Add(patienceInput);
        view.Add(thresholdLabel);
        view.Add(thresholdInput);

        Content = view;
        Focusable = true;
		FocusGained += (s,args) => FocusManager.Instance.SetCurrentFocusView(modelInput);
    }
}
