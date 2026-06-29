using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.ViewModels;
using Wyoming.Net.Satellite.App.Tz.Components;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class WakeSettingsPage : ContentPage, ISelectableView
{
    public WakeSettingsPage(WakeSettingsViewModel vm, View parent)
    {
        var modelLabel = TizenUI.CreateLabel("Model");
        var modelInput = TizenUI.CreateInput(vm, (it) => it.Model, (it, value) => it.Model = value);

        var refracLabel = TizenUI.CreateLabel("Refractory Seconds");
        var refracInput = TizenUI.CreateInput(vm, (it) => it.RefractorySeconds, (it, value) => it.RefractorySeconds = value.ToIntOrDefault());

        var minSpeechLabel = TizenUI.CreateLabel("Min Speech Frames");
        var minSpeechInput = TizenUI.CreateInput(vm, (it) => it.MinSpeechFrames, (it, value) => it.MinSpeechFrames = value.ToIntOrDefault());

        var patienceLabel = TizenUI.CreateLabel("Patience");
        var patienceInput = TizenUI.CreateInput(vm, (it) => it.Patience, (it, value) => it.Patience = value.ToIntOrDefault());

        var thresholdLabel = TizenUI.CreateLabel("Prediction Threshold");
        var thresholdInput = TizenUI.CreateInput(vm, (it) => it.PredictionThreshold, (it, value) => it.PredictionThreshold = value.ToFloatOrDefault(), true);

        modelInput.UpFocusableView = parent;
        modelInput.DownFocusableView = refracInput;

        refracInput.UpFocusableView = modelInput;
        refracInput.DownFocusableView = minSpeechInput;

        minSpeechInput.UpFocusableView = refracInput;
        minSpeechInput.DownFocusableView = patienceInput;

        patienceInput.UpFocusableView = minSpeechInput;
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
        view.Add(refracLabel);
        view.Add(refracInput);
        view.Add(minSpeechLabel);
        view.Add(minSpeechInput);
        view.Add(patienceLabel);
        view.Add(patienceInput);
        view.Add(thresholdLabel);
        view.Add(thresholdInput);

        Content = view;
        Focusable = true;
		FocusGained += (s,args) => FocusManager.Instance.SetCurrentFocusView(modelInput);
    }

    public bool Selected { get; set; }

    public View View => this;
}
