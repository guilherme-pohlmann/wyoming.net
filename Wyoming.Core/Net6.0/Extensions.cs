#if NET6_0

using System.Collections.ObjectModel;

namespace Wyoming.Net.Core.Net6._0;

public static class Extensions
{
    public static readonly ReadOnlyDictionary<string, object> EmptyData = new(new Dictionary<string, object>());
    
    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

    public static ValueTask CancelAsync(this CancellationTokenSource cancellationTokenSource)
    {
        cancellationTokenSource.Cancel();
        return ValueTask.CompletedTask;
    }
}

public sealed class UnreachableException : Exception
{
    public UnreachableException() { }
    
    public UnreachableException(string? message) : base(message) { }
}

#endif