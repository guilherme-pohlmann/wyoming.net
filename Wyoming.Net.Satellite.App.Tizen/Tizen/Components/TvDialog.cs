using System;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;

namespace Wyoming.Net.Satellite.App.Tz.Components;

internal static class TvDialog
{
    public static void ShowOkDialog(string title, string message)
    {
        // Background overlay (dimming layer)
        var overlay = new View
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            BackgroundColor = new Color(0, 0, 0, 0.65f),
        };

        var dialogCard = new View
        {
            Size = new Size2D(900, 450),
            BackgroundColor = new Color("#111827"),
            PositionUsesPivotPoint = true,
            PivotPoint = PivotPoint.Center,
            ParentOrigin = ParentOrigin.Center,
            CornerRadius = 20,
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                CellPadding = new Size2D(0, 30),
            }
        };

        var titleLabel = new TextLabel(title)
        {
            PointSize = 42,
            TextColor = Color.White,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var messageLabel = new TextLabel(message)
        {
            PointSize = 30,
            TextColor = new Color("#D1D5DB"),
            MultiLine = true,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var okButton = new Button
        {
            Text = "OK",
            MinimumSize = new Size2D(260, 100),
            BackgroundColor = new Color("#4F46E5"),
            TextColor = Color.White,
            BorderlineWidth = 0,
        };

        okButton.FocusGained += (s, e) =>
        {
            okButton.Scale = new Vector3(1.1f, 1.1f, 1);
            okButton.BackgroundColor = new Color("#6366F1");
        };

        okButton.FocusLost += (s, e) =>
        {
            okButton.Scale = Vector3.One;
            okButton.BackgroundColor = new Color("#4F46E5");
        };

        var currentFocusView = FocusManager.Instance.GetCurrentFocusView();

        okButton.Clicked += (s, e) =>
        {
            Window.Instance.Remove(overlay);
            FocusManager.Instance.SetCurrentFocusView(currentFocusView);
        };

        dialogCard.Add(titleLabel);
        dialogCard.Add(messageLabel);
        dialogCard.Add(okButton);

        overlay.Add(dialogCard);

        Window.Instance.Add(overlay);
        FocusManager.Instance.SetCurrentFocusView(okButton);
    }
}
