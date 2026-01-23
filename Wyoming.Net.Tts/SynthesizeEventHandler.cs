using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;
using Wyoming.Net.Tts.Backend;
using Wyoming.Net.Tts.Backend.Kokoro;

namespace Wyoming.Net.Tts;

public sealed class SynthesizeEventHandler : AsyncEventHandler
{
    private static readonly Event KokoroAudioStartEvent = new AudioStart()
    {
        Channels = KokoroBackend.Channels,
        Rate = KokoroBackend.SampleRate,
        Timestamp = null,
        Width = KokoroBackend.Width,
    }.ToEvent();

    private static readonly Event KokoroSynthesizeStoppedEvent = new SynthesizeStopped().ToEvent();
    private static readonly Event KokoroAudioStopEvent = new AudioStop().ToEvent();
    
    private IInferenceBackend? inferenceBackend;
    private readonly Info wyomingInfo;
    private readonly WyomingStreamWriter writer;

    private bool isStreaming;
    
    public SynthesizeEventHandler(
        TcpClient client, 
        AsyncTcpServer server,
        ILoggerFactory loggerFactory,
        Info wyomingInfo) 
        : base(client, server, loggerFactory)
    {
        this.wyomingInfo = wyomingInfo;
        writer = new WyomingStreamWriter(client.GetStream());
    }

    protected override async Task<bool> HandleEventAsync(Event ev, CancellationToken cancellationToken)
    {
        if (Describe.IsType(ev.Type))
        {
            await writer.WriteEventAsync(wyomingInfo.ToEvent());
            return true;
        }

        if (Synthesize.IsType(ev.Type))
        {
            if (isStreaming)
            {
                return true;
            }
            
            var synthesize = Synthesize.FromEvent(ev);
            
            EnsureBackend(synthesize.Voice?.Name);
            await HandleSynthesizeAsync(synthesize.Text, cancellationToken);

            return true;
        }
        
        if (SynthesizeStart.IsType(ev.Type))
        {
            var synthesizeStart = SynthesizeStart.FromEvent(ev);
            EnsureBackend(synthesizeStart.Voice?.Name);
            isStreaming = true;
            return true;
        }

        if (SynthesizeChunk.IsType(ev.Type))
        {
            Asserts.IsNotNull(inferenceBackend, "Expected inference backend to not be null at this point");
            var synthesizeChunk = SynthesizeChunk.FromEvent(ev);
            
            await HandleSynthesizeAsync(synthesizeChunk.Text, cancellationToken);
            return true;
        }

        if (SynthesizeStop.IsType(ev.Type))
        {
            isStreaming = false;
            await writer.WriteEventAsync(KokoroSynthesizeStoppedEvent);
        }

        return true;
    }
    
    private async Task HandleSynthesizeAsync(string? text, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            
            await inferenceBackend!.SynthesizeAsync(text, OnSpeechAsync).WaitAsync(cancellationToken);
        }
        finally
        {
            await ReleaseBackendAsync();
        }
    }

    private async Task OnSpeechAsync(Memory<float> samples, int iteration)
    {
        switch (iteration)
        {
            case -1:
                // audio stop
                await writer.WriteEventAsync(KokoroAudioStopEvent);
                return;
            case 0:
                //audio start
                await writer.WriteEventAsync(KokoroAudioStartEvent);
                break;
        }

        var audioBytes = MemoryMarshal.Cast<float, byte>(samples.Span);
        AudioChunk audioChunk = AudioChunk.FromFloatPcm(audioBytes, null, KokoroBackend.SampleRate, KokoroBackend.Channels);
        
        await writer.WriteEventAsync(audioChunk.ToEvent());
    }

    protected override ValueTask OnDisconnectAsync()
    {
        return ReleaseBackendAsync();
    }

    private void EnsureBackend(string? voice)
    {
        Asserts.IsNull(inferenceBackend, "Expected inference backend to be null at this point");
        
        if (inferenceBackend is null)
        {
            voice = string.IsNullOrEmpty(voice) ? "af_alloy" : voice;
            inferenceBackend = new KokoroBackend(voice);
        }
    }
    
    private async ValueTask ReleaseBackendAsync()
    {
        if (inferenceBackend is not null)
        {
            await inferenceBackend.DisposeAsync();
            inferenceBackend = null;
        }
    }
}