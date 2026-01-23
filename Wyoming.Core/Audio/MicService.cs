using Microsoft.Extensions.Logging;

namespace Wyoming.Net.Core.Audio;

public sealed class MicService : TaskLoopRunner
{
    public MicService(IMicInputProvider micProvider, IMicOutputHandler micOutputHandler, ILogger<MicService> logger) 
        : base(logger, TaskLoopRunnerOptions.LongRunning | TaskLoopRunnerOptions.RestartOnFail)
    {
        Provider = micProvider;
        OutputHandler = micOutputHandler;
    }

    public IMicInputProvider Provider { get; }

    public IMicOutputHandler OutputHandler { get; }

    protected override async Task LoopAsync()
    {
        byte[] buffer = new byte[1280 * Provider.Width];

        while(!CancellationTokenSource!.IsCancellationRequested)
        {
            try
            {
                var timestamp = await Provider.ReadAsync(buffer, CancellationTokenSource.Token).ConfigureAwait(false);
                await OutputHandler.OnMicAudioAsync(buffer, timestamp).ConfigureAwait(false);
            }
            catch(OperationCanceledException) when (CancellationTokenSource.IsCancellationRequested)
            {
                logger.LogDebug("MicService cancelled");
                break;
            }
            catch(Exception e)
            {
                logger.LogError(e, "Unexpected error in MicService");
                throw;
            }
        }
    }

    protected override ValueTask OnStopAsync()
    {
        logger.LogDebug("MicService stopping");
        return Provider.StopRecordingAsync();
    }

    protected override ValueTask OnStartAsync()
    {
        logger.LogDebug("MicService starting");
        return Provider.StartRecordingAsync();
    }
}