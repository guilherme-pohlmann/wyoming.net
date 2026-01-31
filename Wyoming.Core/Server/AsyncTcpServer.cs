using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Wyoming.Net.Core.Server;


public delegate AsyncEventHandler EventHandlerFactory(TcpClient client, AsyncTcpServer server, ILoggerFactory loggerFactory);

public sealed class AsyncTcpServer : TaskLoopRunner
{
    private TcpListener? listener;
    private readonly ConcurrentDictionary<int, AsyncEventHandler> handlers = new();
    private readonly EventHandlerFactory handlerFactory;
    private readonly ILoggerFactory loggerFactory;
    private readonly IPEndPoint endpoint;

    public AsyncTcpServer(string host, int port, EventHandlerFactory eventHandlerFactory, ILoggerFactory loggerFactory) 
        : base(loggerFactory.CreateLogger<AsyncTcpServer>(), TaskLoopRunnerOptions.RestartOnFail)
    {
        IPAddress? ipAddress;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            ipAddress = IPAddress.Loopback;
        }
        else if (!IPAddress.TryParse(host, out ipAddress))
        {
            throw new ArgumentException("Invalid host");
        }

        if (port > IPEndPoint.MaxPort || port < IPEndPoint.MinPort)
        {
            throw new ArgumentException("Invalid port");
        }

        endpoint = new IPEndPoint(ipAddress, port);
        handlerFactory = eventHandlerFactory;
        this.loggerFactory = loggerFactory;
    }

    public async Task RunAsync()
    {
        await StartAsync().ConfigureAwait(false);

        try
        {
            await WaitAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (CancellationTokenSource?.IsCancellationRequested ?? false) 
        {
            logger.LogInformation("RunAsync stopping due to Cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RunAsync unexpected error");
        }
        finally
        {
            await StopAsync().ConfigureAwait(false);
        }
    }

    public void NotifyHandlerStopped(AsyncEventHandler handler)
    {
        if(!handlers.TryRemove(handler.GetHashCode(), out _))
        {
            //TODO: memory leak?
            logger.LogError("Failed to remove handler");
        }
    }

    protected override ValueTask OnStartAsync()
    {
        listener = new TcpListener(endpoint);
        listener.Start();
        
        logger.LogInformation("Server started");
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask OnStopAsync()
    {
        listener!.Stop();

        foreach (var kv in handlers)
        {
            await kv.Value.StopAsync();
        }

        handlers.Clear();

#if NET9_0_OR_GREATER
        listener.Dispose();  
#endif
    }

    protected override async Task LoopAsync()
    {
        while (!CancellationTokenSource!.IsCancellationRequested)
        {
            TcpClient client;

            try
            {
                client = await listener!.AcceptTcpClientAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                logger.LogInformation("Client connected: {clientIp}", client.Client.RemoteEndPoint?.ToString());
            }
            catch (OperationCanceledException) when (CancellationTokenSource.IsCancellationRequested)
            {
                logger.LogDebug("AcceptLoopAsync stopping due to Cancellation");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AcceptLoopAsync unexpected error");

                // throw so base class restarts
                throw;
            }

            await HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var socket = client.Client;
        try
        {
            socket.NoDelay = true; // disable Nagle for low-latency control messages
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "HandleClientAsync failed to set socket option");
        }

        var handler = handlerFactory(client, this, loggerFactory);
        handlers[handler.GetHashCode()] = handler;
        await handler.StartAsync();
    }
}
