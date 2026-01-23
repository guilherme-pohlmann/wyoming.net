using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Core.Events;

namespace Wyoming.Net.Satellite;

public sealed class WakeWordSatellite : SatelliteBase, IMicOutputHandler, IWakeWordPredictionHandler
{
    private readonly MicService micService;
    private readonly OpenWakeWordService openWakeWordService;
    
    private long refractoryTimestamp;

    public event Func<Task>? WakeWordDetected; 

    public WakeWordSatellite(
        SatelliteSettings settings,
        OpenWakeWordModels wakeModels,
        ILoggerFactory loggerFactory,
        IMicInputProvider micInputProvider,
        ISpeakerProvider speakerProvider
        ) : base(settings, loggerFactory, speakerProvider)
    {
        openWakeWordService = new OpenWakeWordService(
            wakeModels, 
            this, 
            loggerFactory.CreateLogger<OpenWakeWordService>(), 
            settings.Wake.MaxPatience, 
            settings.Wake.PredictionThreshold);

        micService = new MicService(
            micInputProvider, 
            this, 
            loggerFactory.CreateLogger<MicService>());
    }

    private long RefractoryTimestamp
    {
        get
        {
            return Interlocked.CompareExchange(ref refractoryTimestamp, 0, 0);
        }
        set
        {
            Interlocked.Exchange(ref refractoryTimestamp, value);
        }
    }

    public async ValueTask OnMicAudioAsync(byte[] buffer, long? timestamp)
    {
        if (IsPaused || MicMuted)
        {
            return;
        }

        if (IsStreaming)
        {
            Logger.LogDebug("Sending {samples} to server", buffer.Length);
            
            var chunk = AudioChunk.FromFloatPcm(
                buffer,
                timestamp,
                micService.Provider.Rate,
                micService.Provider.Channels
            );

            await WriteToServerAsync(chunk.ToEvent());
        }
        else
        {
            openWakeWordService.AppendPcm(MemoryMarshal.Cast<byte, float>(buffer));
        }
    }

    public override async Task<bool> EventFromServerAsync(Event ev)
    {
        try
        {
            await ProcessEventAsync(ev);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Event from server failed");
            await OnErrorAsync(e);
        }
        
        return false;
    }

    private async Task ProcessEventAsync(Event ev)
    {
        bool isRunSatellite = false;
        bool isPauseSatellite = false;
        bool isTranscription = false;
        bool isError = false;

        if (RunSatellite.IsType(ev.Type))
        {
            isRunSatellite = true;
        }
        else if (PauseSatellite.IsType(ev.Type))
        {
            isPauseSatellite = true;
        }
        else if (Transcript.IsType(ev.Type))
        {
            isTranscription = true;
        }
        else if (Error.IsType(ev.Type))
        {
            isError = true;
        }

        if (isRunSatellite || isPauseSatellite || isTranscription || isError)
        {
            IsStreaming = false;

            if (isRunSatellite)
            {
                // Go back to wakeword detection
                await RunSatelliteAsync();
            }
            else if (isPauseSatellite)
            {
                await PauseSatelliteAsync();
            }

        }

        await base.EventFromServerAsync(ev);
    }

    private async ValueTask PauseSatelliteAsync()
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        IsStreaming = false;
        MicMuted = true;
        await micService.StopAsync();
        await openWakeWordService.StopAsync();
    }

    private async ValueTask RunSatelliteAsync()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        MicMuted = false;
        await micService.StartAsync();
        await openWakeWordService.StartAsync();
    }

    protected override ValueTask OnServerConnectedAsync()
    {
        Logger.LogInformation("Server connected");
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnServerDisconnectedAsync()
    {
        Logger.LogInformation("Server disconnected");
        return PauseSatelliteAsync();
    }

    async ValueTask IWakeWordPredictionHandler.OnPredictionAsync()
    {
        // we can be paused while there are still wake word tasks in the queue
        if (IsPaused || IsStreaming || string.IsNullOrEmpty(ServerId))
        {
            return;
        }

        long refractory = RefractoryTimestamp;

        if (refractory > 0 && refractory > Stopwatch.GetTimestamp())
        {
            Logger.LogInformation("Skipping wakeword detection, refractory period");
            return;
        }

        if (Settings.Wake.RefractorySeconds.HasValue)
        {
            RefractoryTimestamp = Stopwatch.GetTimestamp() + Settings.Wake.RefractorySeconds.Value;
        }
        else
        {
            RefractoryTimestamp = 0;
        }

        Logger.LogInformation("Wake word detected");
        
        await WriteToServerAsync(new Detection()
        {
            Name = Settings.Wake.Name
        }.ToEvent());

        await SendRunPipelineAsync();
        IsStreaming = true;
        
        if (WakeWordDetected is not null)
        {
            await WakeWordDetected();
        }
    }
}
