using System;
using System.Collections.Generic;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;

namespace Wyoming.Net.Satellite.App.Tz.Components;

internal sealed class TvTabView : View
{
    internal class TabItem : Button
    {
        public TabItem(View child, int index)
        {
            Child = child;
            Index = index;
        }

        public event EventHandler Leave;

        public View Child { get; private set; }

        public int Index { get; private set; }

        public bool Selected { get; set; }

        public void OnLeave()
        {
            Leave?.Invoke(this, EventArgs.Empty);
        }
    }

    private readonly List<TabItem> tabs = new();

    private readonly View body;

    private readonly View header;

    public TvTabView()
    {
        BackgroundColor = TvStyle.MainBackgroundColor;

        body = new View()
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FitToChildren,
            Focusable = true,
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical
            }
        };
        body.FocusGained += OnBodyFocus;

        header = new View()
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FitToChildren,
            Focusable = true,
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Begin,
                VerticalAlignment = VerticalAlignment.Center,

            }
        };

        WidthResizePolicy = ResizePolicyType.FillToParent;
        HeightResizePolicy = ResizePolicyType.FitToChildren;
        Focusable = true;
        Layout = new LinearLayout
        {
            LinearOrientation = LinearLayout.Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Begin,
            VerticalAlignment = VerticalAlignment.Center,

        };

        Add(header);
        Add(body);
        FocusGained += OnFocus;

        body.UpFocusableView = header;
    }

    private void OnBodyFocus(object? sender, EventArgs args)
    {
        foreach (var view in body.Children)
        {
            if (view.Focusable)
            {
                FocusManager.Instance.SetCurrentFocusView(view);
                return;
            }
        }

        var selectedTab = tabs.FirstOrDefault(t => t.Selected);

        if (selectedTab != null)
        {
            FocusManager.Instance.SetCurrentFocusView(selectedTab);
        }
    }

    private void OnFocus(object? sender, EventArgs args)
    {
        if (tabs.Any())
        {
            FocusManager.Instance.SetCurrentFocusView(tabs.First(it => it.Selected));
        }
    }

    private void OnTabFocus(object? sender, EventArgs args)
    {
        if (sender is not TabItem tab)
        {
            return;
        }

        foreach (var t in tabs)
        {
            if (t.Selected)
            {
                t.OnLeave();
                t.Selected = false;
                body.Remove(t.Child);
            }
        }

        tab.Selected = true;
        tab.BorderlineColor = TvStyle.ButtonFocusedBorderlineColor;
        tab.BackgroundColor = TvStyle.ButtonFocusedBackgroundColor;
        tab.DownFocusableView = body;

        tab.DownFocusableView = body;
        body.UpFocusableView = tab;

        body.Add(tab.Child);
    }

    private void OnTabLostFocus(object? sender, EventArgs args)
    {
        var tab = sender as TabItem;

        if (tab is null)
        {
            return;
        }

        var nextFocus = FocusManager.Instance.GetCurrentFocusView();

        if (nextFocus == body && tab.Selected)
        {
            return;
        }

        tab.BorderlineColor = TvStyle.ButtonBorderlineColor;
        tab.BackgroundColor = Color.Transparent;
    }

    public TabItem AddTab(string name, View child)
    {
        var tab = new TabItem(child, tabs.Count)
        {
            Text = name,
            WidthSpecification = 400,
            HeightSpecification = 100,
            Focusable = true,
            FocusNavigationSupport = true,
            BorderlineColor = TvStyle.ButtonBorderlineColor,
            BorderlineWidth = 1,
            TextColor = Color.White,
            WidthResizePolicy = ResizePolicyType.FillToParent,
            Selected = tabs.Count == 0
        };
        tab.FocusGained += OnTabFocus;
        tab.FocusLost += OnTabLostFocus;

        if (tabs.Any())
        {
            var last = tabs.Last();
            tab.LeftFocusableView = last;

            last.RightFocusableView = tab;
        }

        tabs.Add(tab);
        header.Add(tab);

        return tab;
    }
}