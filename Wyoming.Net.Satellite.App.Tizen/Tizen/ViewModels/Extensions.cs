using System;

namespace Wyoming.Net.Satellite.App.Tz.ViewModels;

public static class Extensions
{
    public static int ToIntOrDefault(this string? s)
    {
        if(string.IsNullOrEmpty(s) || !int.TryParse(s, out int i))
        {
            return default;
        }

       return i;
    }

    public static float ToFloatOrDefault(this string? s)
    {
        if(string.IsNullOrEmpty(s) || !float.TryParse(s, out float f))
        {
            return default;
        }

       return f;
    }
}
