using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wyoming.Net.Core;

public static class Asserts
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsTrue(bool condition, string? message = null)
    {
        if(Debugger.IsAttached)
        {
            Debug.Assert(condition, message);
        }
        else if (!condition)
        {
            throw new UnreachableException(message);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsFalse(bool condition, string? message = null)
    {
        if (Debugger.IsAttached)
        {
            Debug.Assert(!condition, message);
        }
        else if (condition)
        {
            throw new UnreachableException(message);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsNotNull(object? obj, string? message = null)
    {
        if (Debugger.IsAttached)
        {
            Debug.Assert(obj is not null, message);
        }
        else if (obj is null)
        {
            throw new UnreachableException(message);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsNull(object? obj, string? message = null)
    {
        if (Debugger.IsAttached)
        {
            Debug.Assert(obj is null, message);
        }
        else if (obj is not null)
        {
            throw new UnreachableException(message);
        }
    }
}
