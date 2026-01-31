#if !NET9_0_OR_GREATER
using System.Text.Json;

namespace Wyoming.Net.Core.Net6._0;

public static class StringConverter
{
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Calculate the required length to avoid resizing
        // In the worst case (all caps), snake_case adds one '_' per char.
        // stackalloc should be safe for typical JSON property lengths (e.g., < 256 chars).
        int maxPossibleLength = input.Length * 2;
        Span<char> destination = stackalloc char[maxPossibleLength];
        
        int pos = 0;
        ReadOnlySpan<char> source = input.AsSpan();

        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];

            if (char.IsUpper(c))
            {
                // Add underscore if it's not the first character
                if (i > 0)
                {
                    destination[pos++] = '_';
                }
                destination[pos++] = char.ToLowerInvariant(c);
            }
            else
            {
                destination[pos++] = c;
            }
        }
        
        return new string(destination.Slice(0, pos));
    }
}

public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => StringConverter.ToSnakeCase(name);
}
#endif