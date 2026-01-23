using Microsoft.Extensions.Logging;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;

namespace Wyoming.Net.Satellite;

internal sealed class PingPongTask : TaskLoopRunner, IAsyncDisposable
{
    private static readonly Event CachedPing = new Ping().ToEvent();

    private const int PingDelaySeconds = 2;
    private const int PongDelaySeconds = 5;
    
    private readonly WyomingStreamWriter writer;
    private readonly ManualResetEventSlim pongEvent = new(false);

    public PingPongTask(WyomingStreamWriter writer, ILogger<PingPongTask> logger) : base(logger)
    {
        this.writer = writer;
    }

    public void Pong()
    {
        pongEvent.Set();
    }

    protected override async Task LoopAsync()
    {
        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(PingDelaySeconds));

            try
            {
                if(CancellationTokenSource is null || CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                await writer.WriteEventAsync(CachedPing);

                await Task.Factory.StartNew(pongEvent.Wait).WaitAsync(TimeSpan.FromSeconds(PongDelaySeconds), CancellationTokenSource.Token).ConfigureAwait(false);
                pongEvent.Reset();
            }
            catch(TimeoutException)
            {
                pongEvent.Set();
                pongEvent.Reset();

                logger.LogInformation("Timeout waiting pong from server");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred on PingPong task");
                throw;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        pongEvent.Dispose();
    }

    protected override ValueTask OnStopAsync()
    {
        pongEvent.Set();
        return base.OnStopAsync();
    }
}
