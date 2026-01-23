using System.Collections.ObjectModel;

namespace Wyoming.Net.Core.Events;

public sealed record Event(string Type, ReadOnlyDictionary<string, object>? Data = null, byte[]? Payload = null)
{
    public bool TryGetDataValue(string key, out object? value)
    {
        value = null;

        if (Data is null)
        {
            return false;
        }

        return Data.TryGetValue(key, out value);
    }

    public T? GetDataValueOrDefault<T>(string key)
    {
        Asserts.IsTrue(Data != null, "Data should not be null at this call site");

        if (Data!.TryGetValue(key, out var value))
        {
            if (value is T cast)
            {
                return cast;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        return default;
    }
}
