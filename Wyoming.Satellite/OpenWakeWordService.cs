using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Core.WebRtc;
using Wyoming.Net.Satellite.ML.Models.OpenWakeWord;
using Wyoming.Net.Satellite.ML.Models.SileroVad;
using Wyoming.Net.Satellite.ML.Models.SileroVad.Onnx;

namespace Wyoming.Net.Satellite;

public readonly struct OpenWakeWordModels
{
    public readonly IEmbeddingModel EmbeddingModel;
    public readonly IMelspectrogramModel MelspectrogramModel;
    public readonly IWakeWordModel WakeWordModel;

    public OpenWakeWordModels(IEmbeddingModel embeddingModel,
        IMelspectrogramModel melspectrogramModel,
        IWakeWordModel wakeWordModel)
    {
        EmbeddingModel = embeddingModel;
        MelspectrogramModel = melspectrogramModel;
        WakeWordModel = wakeWordModel;
    }
}

public sealed class OpenWakeWordService : TaskLoopRunner, IAsyncDisposable
{
    private const int ExpectedSampleSize = MicSettings.SamplesPerChunk;
    private const int SampleWindowSize = 480;

    // Input for Embedding Model
    private readonly int melSpectogramBufferSize;

    // Input por WakeWordModel
    private readonly int embeddingBufferSize;

    private readonly IEmbeddingModel embeddingModel;
    private readonly IMelspectrogramModel melspectrogramModel;
    private readonly IWakeWordModel wakeWordModel;
    private readonly SlidingWindowPcmBuffer melBuffer;
    private readonly SlidingWindowPcmBuffer embeddingBuffer;
    private readonly SlidingWindowPcmBuffer rawAudioBuffer = new(ExpectedSampleSize + SampleWindowSize);
    private readonly IWakeWordPredictionHandler predictionHandler;
    private readonly WebRtcVad? webRtcVad;
    private readonly SileroVad sileroVad = new SileroVad(0.5f, 16000, 250, 100);
    private readonly SileroVadModel sileroVadModel;

    private readonly Channel<AudioTask<float>> channel;
    
    private int silenceFrames = 0;

    public OpenWakeWordService(
        OpenWakeWordModels models,
        IWakeWordPredictionHandler predictionHandler,
        ILogger<OpenWakeWordService> logger)
        : base(logger, TaskLoopRunnerOptions.RestartOnFail)
    {
        #if ANDROID
        sileroVadModel = new SileroVadModel(DroidAssetReader.ReadAsset(Android.App.Application.Context.Assets!,"silero_vad.onnx"));  
        #endif
        
        embeddingModel = models.EmbeddingModel;
        melspectrogramModel = models.MelspectrogramModel;
        wakeWordModel = models.WakeWordModel;
        embeddingBufferSize = models.WakeWordModel.FlatShapeSize;
        embeddingBuffer = new SlidingWindowPcmBuffer(embeddingBufferSize);
        melSpectogramBufferSize = models.EmbeddingModel.FlatShapeSize;
        melBuffer = new SlidingWindowPcmBuffer(melSpectogramBufferSize);
        
        this.predictionHandler = predictionHandler;

        channel = Channel.CreateBounded<AudioTask<float>>(new BoundedChannelOptions(32)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest,
            AllowSynchronousContinuations = false
        });

        if (SatelliteSettings.Vad.Enabled && SatelliteSettings.Vad.Type.HasFlag(VadSettings.VadType.WebRtc))
        {
            webRtcVad = new WebRtcVad()
            {
                SampleRate = MicSettings.Rate,
                Mode = SatelliteSettings.Vad.WebRtcMode
            };
        }
    }

    public void AppendPcm(ReadOnlySpan<float> samples)
    {
        if (samples.Length != ExpectedSampleSize)
        {
            throw new ArgumentException($"Samples must be of size {ExpectedSampleSize}");
        }

        //TODO: move audio processing here (silence + webrtc vad) and avoid adding silence audio tasks
        rawAudioBuffer.Append(samples, SampleWindowSize);
        channel.Writer.TryWrite(new AudioTask<float>(rawAudioBuffer.Span));
    }

    protected override async Task LoopAsync()
    {
        int patience = SatelliteSettings.Wake.MaxPatience;
        float predictionThreshold = SatelliteSettings.Wake.PredictionThreshold;

        while (!CancellationTokenSource!.IsCancellationRequested)
        {
            if (!await channel.Reader.WaitToReadAsync(CancellationTokenSource!.Token))
            {
                continue;
            }
            
            using var chunk = await channel.Reader.ReadAsync(CancellationTokenSource!.Token);
            // var w = Stopwatch.StartNew();
            // SileroVadEvent(chunk.Buffer);
            //
            // w.Stop();
            // logger.LogInformation($"Silero processed in {w.ElapsedMilliseconds} ms");
            //
            // if (ev.Contains(SileroVad.VadEvent.SpeechStart))
            // {
            //     if (ev.Contains(SileroVad.VadEvent.SpeechEnd))
            //     {
            //         logger.LogInformation("start and end");    
            //     }
            //     else
            //     {
            //         logger.LogInformation("start");
            //     }
            // }
            // else if(ev.Contains(SileroVad.VadEvent.SpeechEnd))
            // {
            //     logger.LogInformation("end only");
            // }
            
            float prediction = Predict(chunk.Buffer.Span);
            
            logger.LogDebug("Prediction: {prediction}", prediction);
            
            if (patience > 0)
            {
                patience--;
                continue;
            }
            
            if (patience == 0 && prediction >= predictionThreshold && !CancellationTokenSource.IsCancellationRequested)
            {
                patience = SatelliteSettings.Wake.MaxPatience;
                await predictionHandler.OnPredictionAsync();
            }
        }
    }

    private List<SileroVad.VadEvent> SileroVadEvent(Memory<float> samples)
    {
        Span<float> predictions =  stackalloc float[SileroVadModel.GetNumberOfPredictions(samples.Length)];
        sileroVadModel.BatchPredict(samples, predictions);
        
        return sileroVad.HasDetectedSpeech(predictions);
    }
    
    private float Predict(ReadOnlySpan<float> samples)
    {
        // if ((SatelliteSettings.Vad.UseEnergyGate && IsSilence(samples)) || (webRtcVad is not null && !VadIsSpeech(samples)))
        // {
        //     silenceFrames = Math.Max(silenceFrames, 0);
        //
        //     if(++silenceFrames == 5)
        //     {
        //         melBuffer.Clear();
        //         embeddingBuffer.Clear();
        //     }
        //     return 0f;
        // }
        //
        // silenceFrames = 0;
        
        // samples -> MelspectrogramModel -> EmbeddingModel -> WakeWordModel

        Span<float> melOutputBuffer = stackalloc float[melspectrogramModel.FlattenedOutputSize];
        melspectrogramModel.GenerateSpectrogram(samples, melOutputBuffer);

        melBuffer.Append(melOutputBuffer, melSpectogramBufferSize - melOutputBuffer.Length);

        Span<float> embeddingOutputBuffer = stackalloc float[embeddingModel.FlattenedOutputSize];
        embeddingModel.GenerateAudioEmbeddings(melBuffer.Span, embeddingOutputBuffer);
        embeddingBuffer.Append(embeddingOutputBuffer, embeddingBufferSize - embeddingOutputBuffer.Length);

        float prediction = wakeWordModel.Predict(embeddingBuffer.Span);

        return prediction;
    }

    private static bool IsSilence(ReadOnlySpan<float> samples)
    {
        float energy = 0f;
        int zeroCrossings = 0;

        for (int i = 1; i < samples.Length; i++)
        {
            energy += samples[i] * samples[i];

            if ((samples[i] > 0) != (samples[i - 1] > 0))
            {
                zeroCrossings++;
            }
        }

        energy /= samples.Length;
        float zcr = (float)zeroCrossings / samples.Length;

        // Low energy = silence; high energy + very high ZCR = noise, not speech
        if (energy < SatelliteSettings.Vad.EnergyGateThreshold)
        {
            return true;
        }

        if (zcr > SatelliteSettings.Vad.EnergyGateZcr)
        {
            return true; // likely noise, not speech
        }

        return false;
    }

    private bool VadIsSpeech(ReadOnlySpan<float> samples)
    {
        Span<byte> frames = stackalloc byte[samples.Length * 2];
        AudioOp.FloatToPcm16(samples, frames);
        
        ReadOnlySpan<short> pcm = MemoryMarshal.Cast<byte, short>(frames);
        
        const int chunkSize = 480; // 30ms at 16kHz
        bool hasSpeech = false;
        
        for (int i = 0; i + chunkSize <= pcm.Length; i += chunkSize)
        {
            if (webRtcVad!.Process(pcm.Slice(i, chunkSize)))
            {
                hasSpeech = true;
                break;
            }
        }
        
        if (!hasSpeech)
        {
            int remaining = pcm.Length % chunkSize; // 1760 % 480 = 320 (20ms)
            hasSpeech = webRtcVad!.Process(pcm.Slice(pcm.Length - remaining, remaining));
        }

        return hasSpeech;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync();

            embeddingModel.Dispose();
            melspectrogramModel.Dispose();
            wakeWordModel.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error disposing openwakeword service");
        }
    }
}

sealed class SlidingWindowPcmBuffer
{
    private readonly float[] buffer;

    public SlidingWindowPcmBuffer(int maxSize)
    {
        buffer = new float[maxSize];
    }

    public void Append(ReadOnlySpan<float> newData, int windowSize)
    {
        var span = buffer.AsSpan();
        span.Slice(buffer.Length - windowSize).CopyTo(span); // Move old data to start

        newData.CopyTo(span.Slice(windowSize));  // Put new data at the end
    }

    public ReadOnlySpan<float> Span => buffer.AsSpan();

    public void Clear()
    {
        buffer.AsSpan().Clear();
    }
}

sealed class AudioTask<T> : IDisposable
    where T : struct
{
    private readonly int size;
    private readonly T[] chunk;

    public AudioTask(ReadOnlySpan<T> chunk)
    {
        size = chunk.Length;
        this.chunk = ArrayPool<T>.Shared.Rent(size);
        chunk.CopyTo(this.chunk);
    }

    public Memory<T> Buffer => new(chunk, 0, size);

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(chunk);
    }
}