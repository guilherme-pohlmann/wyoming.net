using System.Collections.Generic;
using System.Linq;
using Tizen.Applications;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite.App.Tz.Components;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class StateConfigurationPage : ContentPage, ISelectableView
{
    private readonly View _clipContainer;
    private readonly View _content;

    public StateConfigurationPage(SatelliteSettingsViewModel vm, View parent, IEnumerable<ApplicationInfo> installedApps)
    {
        _clipContainer = new View
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            ClippingMode = ClippingModeType.ClipChildren,
        };

        _content = new View
        {
            WidthSpecification = LayoutParamPolicies.MatchParent,
            HeightSpecification = LayoutParamPolicies.WrapContent,
            Padding = new Extents(200, 200, 20, 20),
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };
        _clipContainer.Add(_content);

        var description = new TextLabel("Mark apps as Inactive to automatically stop the satellite when they are in the foreground.")
        {
            PointSize = 22,
            TextColor = new Color("#9CA3AF"),
            Margin = new Extents(0, 0, 0, 30),
            Focusable = false,
            MultiLine = true,
            WidthSpecification = LayoutParamPolicies.MatchParent,
            HeightSpecification = LayoutParamPolicies.WrapContent,
        };
        _content.Add(description);

        var intervalLabel = TizenUI.CreateLabel("Watcher Interval (seconds)");
        var intervalInput = TizenUI.CreateInput(vm.StateConfiguration, (it) => it.WatcherIntervalSeconds, (it, value) => it.WatcherIntervalSeconds = value.ToIntOrDefault());
        intervalInput.UpFocusableView = parent;
        intervalInput.FocusGained += (s, e) => EnsureVisible(intervalInput);

        _content.Add(intervalLabel);
        _content.Add(intervalInput);

        var apps = installedApps.Where(a => !a.IsNoDisplay
                        && a.ApplicationId != Constants.UiAppId
                        && a.ApplicationId != Constants.ServiceAppId
                        && a.ApplicationId != Constants.ProfilerAppId
                        && !string.IsNullOrEmpty(a.ApplicationId))
            .OrderBy(a => a.Label ?? a.ApplicationId)
            .ToList();

        View? previousRow = null;

        foreach (var appInfo in apps)
        {
            var row = new View
            {
                WidthSpecification = LayoutParamPolicies.MatchParent,
                HeightSpecification = 80,
                Margin = new Extents(0, 0, 0, 10),
                Focusable = true,
                BorderlineWidth = 2,
                BorderlineColor = TvStyle.ButtonBorderlineColor,
                Layout = new LinearLayout
                {
                    LinearOrientation = LinearLayout.Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Begin,
                    CellPadding = new Size2D(20, 0),
                }
            };

            var label = new TextLabel(appInfo.ApplicationId + (string.IsNullOrEmpty(appInfo.Label) ? string.Empty : $" ({appInfo.Label})"))
            {
                PointSize = 24,
                TextColor = new Color("#E5E7EB"),
                Focusable = false,
                WidthSpecification = 800,
                VerticalAlignment = VerticalAlignment.Center,
            };

            string capturedAppId = appInfo.ApplicationId;
            bool isUnactive = vm.StateConfiguration.UnactiveApps.Contains(capturedAppId);

            var stateLabel = new TextLabel(isUnactive ? "Inactive" : "Active")
            {
                PointSize = 22,
                Focusable = false,
                WidthSpecification = 250,
                HeightSpecification = 70,
                BackgroundColor = isUnactive ? new Color("#DC2626") : new Color("#1F2937"),
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            void ToggleState()
            {
                if (vm.StateConfiguration.UnactiveApps.Contains(capturedAppId))
                {
                    vm.StateConfiguration.UnactiveApps.Remove(capturedAppId);
                    stateLabel.Text = "Active";
                    stateLabel.BackgroundColor = new Color("#1F2937");
                }
                else
                {
                    vm.StateConfiguration.UnactiveApps.Add(capturedAppId);
                    stateLabel.Text = "Inactive";
                    stateLabel.BackgroundColor = new Color("#DC2626");
                }
            }

            row.KeyEvent += (s, e) =>
            {
                if (e.Key.State == Key.StateType.Down
                    && (e.Key.KeyPressedName == "Return" || e.Key.KeyPressedName == "Select"))
                {
                    ToggleState();
                    return true;
                }
                return false;
            };

            row.FocusGained += (s, e) =>
            {
                row.BorderlineColor = TvStyle.ButtonFocusedBorderlineColor;
                EnsureVisible(row);
            };

            row.FocusLost += (s, e) =>
            {
                row.BorderlineColor = TvStyle.ButtonBorderlineColor;
            };

            if (previousRow != null)
            {
                row.UpFocusableView = previousRow;
                previousRow.DownFocusableView = row;
            }
            else
            {
                row.UpFocusableView = intervalInput;
                intervalInput.DownFocusableView = row;
            }

            row.Add(label);
            row.Add(stateLabel);
            _content.Add(row);
            previousRow = row;
        }

        Content = _clipContainer;
        Focusable = true;

        FocusGained += (s, args) =>
        {
            FocusManager.Instance.SetCurrentFocusView(intervalInput);
        };
    }

    private void EnsureVisible(View target)
    {
        float clipHeight = _clipContainer.SizeHeight;
        if (clipHeight <= 0) return;

        float targetY = target.PositionY;
        float targetHeight = target.SizeHeight;
        float contentOffset = _content.PositionY;

        float visibleTop = -contentOffset;
        float visibleBottom = visibleTop + clipHeight;

        float margin = 20f;

        if (targetY + targetHeight + margin > visibleBottom)
        {
            _content.PositionY = -(targetY + targetHeight + margin - clipHeight);
        }
        else if (targetY - margin < visibleTop)
        {
            _content.PositionY = -(targetY - margin);
        }
    }

    public bool Selected { get; set; }

    public View View => this;
}
