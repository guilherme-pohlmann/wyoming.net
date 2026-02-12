using System;
using Microsoft.Extensions.Logging;
using TizenLog = Tizen.Log;

namespace Wyoming.Net.Satellite.App.Tz.Platform;

internal class TizenLogger : ILogger, IDisposable
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose()
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel == LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        TizenLog.Info("WYOMING", msg);
    }
}

internal sealed class TizenLogger<TCategory> : TizenLogger, ILogger<TCategory>
{
}

internal sealed class TizenLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TizenLogger();
    }

    public void Dispose()
    {
    }
}
