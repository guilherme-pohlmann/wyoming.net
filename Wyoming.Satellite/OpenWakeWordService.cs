using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Threading.Channels;
using Wyoming.Net.Core;
using Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

namespace Wyoming.Net.Satellite;

public readonly struct OpenWakeWordModels
{
    public readonly EmbeddingModel EmbeddingModel;
    public readonly MelspectrogramModel MelspectrogramModel;
    public readonly WakeWordModel WakeWordModel;

    public OpenWakeWordModels(EmbeddingModel embeddingModel,
        MelspectrogramModel melspectrogramModel,
        WakeWordModel wakeWordModel)
    {
        EmbeddingModel = embeddingModel;
        MelspectrogramModel = melspectrogramModel;
        WakeWordModel = wakeWordModel;
    }
}

public sealed class OpenWakeWordService : TaskLoopRunner, IAsyncDisposable
{
    private const int ExpectedSampleSize = 1280;
    private const int SampleWindowSize = 480;

    // Input for Embedding Model
    private const int MelSpectogramBufferSize = EmbeddingModel.FlatShapeSize;

    // Input por WakeWordModel
    private const int EmbeddingBufferSize = WakeWordModel.FlatShapeSize;

    private readonly EmbeddingModel embeddingModel;
    private readonly MelspectrogramModel melspectrogramModel;
    private readonly WakeWordModel wakeWordModel;
    private readonly SlidingWindowPcmBuffer melBufferRing = new(MelSpectogramBufferSize);
    private readonly SlidingWindowPcmBuffer embeddingBuferRing = new(EmbeddingBufferSize);
    private readonly SlidingWindowPcmBuffer rawAudioBuffer = new(ExpectedSampleSize + SampleWindowSize);
    private readonly int maxPatience = 15;
    private readonly float predictionThreshold = 0.5f;
    private readonly IWakeWordPredictionHandler predictionHandler;

    private readonly Channel<AudioTask<float>> channel;

    public OpenWakeWordService(
        OpenWakeWordModels models,
        IWakeWordPredictionHandler predictionHandler,
        ILogger<OpenWakeWordService> logger,
        int maxPatience,
        float predictionThreshold) 
        : base(logger, TaskLoopRunnerOptions.RestartOnFail)
    {
        this.embeddingModel = models.EmbeddingModel;
        this.melspectrogramModel = models.MelspectrogramModel;
        this.wakeWordModel = models.WakeWordModel;
        this.predictionThreshold = predictionThreshold;
        this.maxPatience = maxPatience;
        this.predictionHandler = predictionHandler;

        this.channel = Channel.CreateUnbounded<AudioTask<float>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
    }

    public void AppendPcm(ReadOnlySpan<float> samples)
    {
        if (samples.Length != ExpectedSampleSize)
        {
            throw new ArgumentException($"Samples must be of size {ExpectedSampleSize}");
        }

        rawAudioBuffer.Append(samples, SampleWindowSize);
        channel.Writer.TryWrite(new AudioTask<float>(rawAudioBuffer.Span));
    }

    protected override async Task LoopAsync()
    {
        int patience = maxPatience;

        while (!CancellationTokenSource!.IsCancellationRequested)
        {
            if (!await channel.Reader.WaitToReadAsync(CancellationTokenSource!.Token))
            {
                continue;
            }
            
            using var chunk = await channel.Reader.ReadAsync(CancellationTokenSource!.Token);
            float prediction = Predict(chunk.Buffer.Span);

            logger.LogDebug("Prediction: {prediction}", prediction);
            
            if (patience > 0)
            {
                patience--;
                continue;
            }

            if (patience == 0 && prediction >= predictionThreshold && !CancellationTokenSource.IsCancellationRequested)
            {
                patience = maxPatience;
                await predictionHandler.OnPredictionAsync();
            }
        }
    }

    private float Predict(ReadOnlySpan<float> samples)
    {
        // samples -> MelspectrogramModel -> EmbeddingModel -> WakeWordModel

        var melOutput = melspectrogramModel.GenerateSpectogram(samples);

        Span<float> melOutputBuffer = stackalloc float[melOutput.FlattenedLength];
        melOutput.FlattenTo(melOutputBuffer);

        melBufferRing.Append(melOutputBuffer, MelSpectogramBufferSize - melOutput.FlattenedLength);

        var embeddingOutput = embeddingModel.GenerateAudioEmbeddings(melBufferRing.Span);

        Span<float> embeddingOutputBuffer  = stackalloc float[embeddingOutput.FlattenedLength];
        embeddingOutput.FlattenTo(embeddingOutputBuffer);

        embeddingBuferRing.Append(embeddingOutputBuffer, EmbeddingBufferSize - embeddingOutput.FlattenedLength);

        float prediction = wakeWordModel.Predict(embeddingBuferRing.Span);

        return prediction;
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

    ~AudioTask()
    {
        Dispose(false);
    }

    public Memory<T> Buffer => new(chunk, 0, size);

    private void Dispose(bool disposing)
    {
        ArrayPool<T>.Shared.Return(chunk);

        if(disposing)
        {
            GC.SuppressFinalize(this);
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}