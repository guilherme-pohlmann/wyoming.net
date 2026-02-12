using System;
using System.Linq.Expressions;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

internal static class TizenUI
{
    public static TextLabel CreateLabel(string text)
    {
        var label = new TextLabel(text)
        {
            PointSize = 28,
            TextColor = new Color("#E5E7EB"),
            Margin = new Extents(0, 0, 0, 30),
            Focusable = false,
            HorizontalAlignment = HorizontalAlignment.Begin,
        };

        return label;
    }

    public static TextField CreateInput<TTarget, TData>(TTarget target, Func<TTarget, TData> getter, Action<TTarget, string> setter)
    {
        var input = new TextField
        {
            PointSize = 28,
            WidthResizePolicy = ResizePolicyType.FillToParent,
            BackgroundColor = new Color("#1F2937"),
            Margin = new Extents(0, 0, 0, 40),
            BorderlineColor = new Color("#374151"),
            BorderlineWidth = 2,
            Padding = new Extents(30, 30, 20, 20),
            Text = getter(target)?.ToString(),
            Focusable = true,
            TextColor = new Color("#E5E7EB")
        };

        input.FocusGained += (s, e) =>
        {
            input.BorderlineColor = new Color("#6366F1");
            input.BackgroundColor = new Color("#111827");
            input.Scale = new Vector3(1.05f, 1.05f, 1);
        };

        input.FocusLost += (s, e) =>
        {
            input.BorderlineColor = new Color("#374151");
            input.BackgroundColor = new Color("#1F2937");
            input.Scale = Vector3.One;
        };


        input.TextChanged += (s, args) =>
        {
            setter(target, args.TextField.Text);
        };

        return input;
    }
}
