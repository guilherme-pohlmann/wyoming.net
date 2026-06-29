using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;

namespace Wyoming.Net.Satellite.App.Tz.Components;

public interface ISelectableView
{
    public bool Selected { get; set; }

    public View View { get; }
}

internal sealed class TvTabView : View
{
    internal class TabItem : Button
    {
        public TabItem(ISelectableView child, int index)
        {
            Child = child;
            Index = index;
        }

        public event EventHandler Leave;

        public ISelectableView Child { get; private set; }

        public int Index { get; private set; }

        public bool Selected
        {
            get
            {
                return Child.Selected;
            }
            set
            {
                Child.Selected = value;
            }
        }

        public void OnLeave()
        {
            Leave?.Invoke(this, EventArgs.Empty);
        }
    }

    private const int HeaderExpandedWidth = 400;
    private const int HeaderCollapsedWidth = 80;

    private readonly List<TabItem> tabs = new();

    private readonly View body;

    private readonly View header;

    public TvTabView()
    {
        BackgroundColor = TvStyle.MainBackgroundColor;

        body = new View()
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            Weight = 1,
            Focusable = true,
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical
            }
        };
        body.FocusGained += OnBodyFocus;

        header = new View()
        {
            WidthSpecification = HeaderCollapsedWidth,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            Focusable = true,
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Begin,
                VerticalAlignment = VerticalAlignment.Top,

            },
            BorderlineColor = TvStyle.ButtonBorderlineColor,
            BorderlineWidth = 1,
        };

        WidthResizePolicy = ResizePolicyType.FillToParent;
        HeightResizePolicy = ResizePolicyType.FillToParent;
        Focusable = true;
        Layout = new LinearLayout
        {
            LinearOrientation = LinearLayout.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Begin,
            VerticalAlignment = VerticalAlignment.Top,

        };

        Add(header);
        Add(body);
        FocusGained += OnFocus;

        body.LeftFocusableView = header;
    }

    public View Body => body;

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

    private void ExpandHeader()
    {
        header.WidthSpecification = HeaderExpandedWidth;
        foreach (var t in tabs)
        {
            t.Text = t.Text; // force relayout
        }
    }

    private void CollapseHeader()
    {
        header.WidthSpecification = HeaderCollapsedWidth;
    }

    private void OnTabFocus(object? sender, EventArgs args)
    {
        if (sender is not TabItem tab)
        {
            return;
        }

        ExpandHeader();

        foreach (var t in tabs)
        {
            if (t.Selected)
            {
                t.OnLeave();
                t.Selected = false;
                body.Remove(t.Child.View);
            }
        }

        tab.Selected = true;
        tab.BorderlineColor = TvStyle.ButtonFocusedBorderlineColor;
        tab.BackgroundColor = TvStyle.ButtonFocusedBackgroundColor;
        tab.RightFocusableView = body;
        body.LeftFocusableView = tab;

        body.Add(tab.Child.View);
        SetLeftFocusToHeader(tab.Child.View, tab);
    }

    private void SetLeftFocusToHeader(View view, TabItem tab)
    {
        if (view.Focusable)
        {
            view.LeftFocusableView = tab;
        }

        foreach (var child in view.Children)
        {
            SetLeftFocusToHeader(child, tab);
        }
    }

    private void OnTabLostFocus(object? sender, EventArgs args)
    {
        var tab = sender as TabItem;

        if (tab is null)
        {
            return;
        }

        var nextFocus = FocusManager.Instance.GetCurrentFocusView();

        // Focus moving to another tab — keep expanded, just update style
        if (nextFocus is TabItem)
        {
            tab.BorderlineColor = TvStyle.ButtonBorderlineColor;
            tab.BackgroundColor = Color.Transparent;
            return;
        }

        // Focus leaving the header entirely (into body) — collapse
        CollapseHeader();

        tab.BorderlineColor = TvStyle.ButtonBorderlineColor;
        tab.BackgroundColor = Color.Transparent;
    }

    public TabItem AddTab(string name, ISelectableView child)
    {
        var tab = new TabItem(child, tabs.Count)
        {
            Text = name,
            WidthSpecification = LayoutParamPolicies.MatchParent,
            HeightSpecification = 80,
            Focusable = true,
            FocusNavigationSupport = true,
            BorderlineColor = TvStyle.ButtonBorderlineColor,
            BorderlineWidth = 1,
            TextColor = Color.White,
            //WidthResizePolicy = ResizePolicyType.FillToParent,
            TextAlignment = HorizontalAlignment.Center,
            Selected = tabs.Count == 0
        };
        tab.FocusGained += OnTabFocus;
        tab.FocusLost += OnTabLostFocus;

        if (tabs.Any())
        {
            var last = tabs.Last();
            tab.UpFocusableView = last;

            last.DownFocusableView = tab;
        }

        tabs.Add(tab);
        header.Add(tab);

        return tab;
    }
}