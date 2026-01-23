using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using Wyoming.Net.Core.Events;

namespace Wyoming.Net.Core.Server;

public abstract class AsyncEventHandler : TaskLoopRunner
{
    private readonly WyomingStreamReader streamReader;
    private readonly WyomingEventReader eventReader;
    private readonly TcpClient client;
    private readonly AsyncTcpServer server;
    private bool wasStopped;

    protected AsyncEventHandler(TcpClient client, AsyncTcpServer server, ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<AsyncEventHandler>())
    {
        streamReader = new WyomingStreamReader(client.GetStream(), loggerFactory.CreateLogger<WyomingStreamReader>());
        eventReader = new WyomingEventReader(streamReader, loggerFactory.CreateLogger<WyomingEventReader>());
        this.server = server;
        this.client = client;
    }

    protected override async Task LoopAsync()
    {
        try
        {
            while (!CancellationTokenSource!.IsCancellationRequested)
            {
                Event? ev = await eventReader.ReadEventAsync(CancellationTokenSource.Token).ConfigureAwait(false);

                if (ev == null)
                {
                    logger.LogInformation("Received null event, stopping event handler");
                    break;
                }

                if (!Pong.IsType(ev.Type))
                {
                    logger.LogInformation("Processing: {evType}", ev.Type);
                }

                if (!await HandleEventAsync(ev, CancellationTokenSource.Token).ConfigureAwait(false))
                {
                    logger.LogInformation("Failed to handle event, stopping event handler");
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (CancellationTokenSource?.IsCancellationRequested ?? false)
        {
            logger.LogInformation("Event handler stopping due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Event handler stopping due to error");
            await StopAsync();
        }
        finally
        {
            logger.LogInformation("Client disconnecting: {clientIp}", client.Client.RemoteEndPoint?.ToString());
            server.NotifyHandlerStopped(this);
            await OnDisconnectAsync();
        }
    }

    protected override ValueTask OnStopAsync()
    {
        try
        {
            client.Close();
            client.Dispose();
            wasStopped = true;
            streamReader.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during dispose");
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnStartAsync()
    {
        if(wasStopped)
        {
            throw new InvalidOperationException("Cannot restart a stopped handler");
        }

        return ValueTask.CompletedTask;
    }

    protected abstract Task<bool> HandleEventAsync(Event ev, CancellationToken cancellationToken);

    protected abstract ValueTask OnDisconnectAsync();
}