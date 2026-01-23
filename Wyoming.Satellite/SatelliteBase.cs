using Microsoft.Extensions.Logging;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;

namespace Wyoming.Net.Satellite;

public abstract class SatelliteBase
{
    private readonly ISpeakerProvider speakerProvider;
    private readonly ILoggerFactory loggerFactory;

    private string? serverId;
    private WyomingStreamWriter? writer;
    private PingPongTask? pingPongTask;
    private int isStreaming;
    private int isPaused = 1; // new instance start as paused
    private int micMuted;

    public event Action? StateChanged;

    public event Func<Exception, Task>? SatelliteError;

    public SatelliteBase(
        SatelliteSettings settings,
        ILoggerFactory loggerFactory,
        ISpeakerProvider speakerProvider)
    {
        this.Settings = settings;
        this.speakerProvider = speakerProvider;
        this.loggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger(GetType());
    }

    protected ILogger Logger { get; }

    public bool IsStreaming
    {
        get
        {
            return Interlocked.CompareExchange(ref isStreaming, 0, 0) == 1;
        }
        set
        {
            Logger.LogInformation("IsStreaming new state: {state}", value ? "Streaming" : "NotStreaming");
            Interlocked.Exchange(ref isStreaming, value ? 1 : 0);
            
            OnStateChanged();
        }
    }

    public bool IsPaused
    {
        get
        {
            return Interlocked.CompareExchange(ref isPaused, 0, 0) == 1;
        }
        set
        {
            Interlocked.Exchange(ref isPaused, value ? 1 : 0);
            OnStateChanged();
        }
    }

    public bool MicMuted
    {
        get
        {
            return Interlocked.CompareExchange(ref micMuted, 0, 0) == 1;
        }
        set
        {
            Interlocked.Exchange(ref micMuted, value ? 1 : 0);
            OnStateChanged();
        }
    }

    public bool IsRunning => !IsPaused;

    public string? ServerId
    {
        get => serverId;
        private set
        {
            serverId = value;
            OnStateChanged();
        }
    }

    public SatelliteSettings Settings { get; }

    public virtual async Task<bool> EventFromServerAsync(Event ev)
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
        if (Ping.IsType(ev.Type))
        {
            var ping = Ping.FromEvent(ev);
            await WriteToServerAsync(Pong.FromPing(ping).ToEvent());

            Asserts.IsNotNull(pingPongTask, "PingPongTask should not be null at this point");

            if (!pingPongTask!.IsRunning)
            {
                await pingPongTask.StartAsync();
                Logger.LogDebug("Ping enabled");
            }
        }

        else if (Pong.IsType(ev.Type))
        {
            Asserts.IsNotNull(pingPongTask, "PingPongTask should not be null at this point");
            pingPongTask!.Pong();
        }

        else if (AudioStart.IsType(ev.Type))
        {
            var audioStart = AudioStart.FromEvent(ev);
            await speakerProvider.StartAsync(audioStart.Rate, audioStart.Width, audioStart.Channels);
        }

        else if (AudioChunk.IsType(ev.Type))
        {
            var audioChunk = AudioChunk.FromEvent(ev);

            Asserts.IsTrue(audioChunk.Rate == speakerProvider.Rate, "AudioChunk.Rate should match speakerProvider.Rate at this point");
            Asserts.IsTrue(audioChunk.Width == speakerProvider.Width, "AudioChunk.Width should match speakerProvider.Width at this point");
            Asserts.IsTrue(audioChunk.Channels == speakerProvider.Channels, "AudioChunk.Channels should match speakerProvider.Channels at this point");

            await speakerProvider.PlayAsync(audioChunk.Audio, audioChunk.Timestamp);
        }

        else if (AudioStop.IsType(ev.Type))
        {
            await speakerProvider.StopAsync();
        }

        else if (Error.IsType(ev.Type))
        {
            var error = Error.FromEvent(ev);
            Logger.LogError("Error from server. Code: {code} - Text: {text}", error.Code, error.Text);

            await ClearServerAsync();
        }
    }
    
    public async ValueTask SetServerAsync(string serverId, WyomingStreamWriter writer)
    {
        ServerId = serverId;
        this.writer = writer;
        Logger.LogDebug("Server set: {serverId}", serverId);

        if (pingPongTask != null)
        {
            await pingPongTask.DisposeAsync();
        }
        pingPongTask = new PingPongTask(writer, loggerFactory.CreateLogger<PingPongTask>());

        await OnServerConnectedAsync();
    }

    public async ValueTask ClearServerAsync()
    {
        ServerId = string.Empty;
        writer = null;

        Logger.LogDebug("Server disconnected: {serverId}", ServerId);

        if (pingPongTask is not null && pingPongTask.IsRunning)
        {
            await pingPongTask.StopAsync();
        }

        await OnServerDisconnectedAsync();
    }

    protected async Task WriteToServerAsync(Event ev)
    {
        if (writer is null)
        {
            return;
        }

        try
        {
            await writer.WriteEventAsync(ev);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to write to server");
            throw;
        }
    }

    protected async Task SendRunPipelineAsync()
    {
        string startStage;
        string endStage;
        bool restartOnEnd;

        if (Settings.Wake.Enabled)
        {
            // Local wakeword detection
            startStage = PipelineStage.ASR;
            restartOnEnd = false;
        }
        else
        {
            // Remote wakeword detection
            startStage = PipelineStage.Wake;
            restartOnEnd = Settings.Vad.Enabled;
        }

        if (Settings.Snd.Enabled)
        {
            // Play TTS response
            endStage = PipelineStage.TTS;
        }
        else
        {
            // No audio output
            endStage = PipelineStage.Handle;
        }

        var runPipeline = new RunPipeline(startStage, endStage)
        {
            //TODO: missing fields?
            RestartOnEnd = restartOnEnd,
            WakeWordName = Settings.Wake.Name,
        };

        await WriteToServerAsync(runPipeline.ToEvent());
    }

    // abstract members
    protected abstract ValueTask OnServerConnectedAsync();

    protected abstract ValueTask OnServerDisconnectedAsync();

    private void OnStateChanged()
    {
        StateChanged?.Invoke();
    }

    protected async Task OnErrorAsync(Exception ex)
    {
        if (SatelliteError is not null)
        {
            await SatelliteError(ex);
        }
    }
}
