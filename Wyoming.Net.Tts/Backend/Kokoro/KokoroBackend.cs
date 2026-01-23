using System.Buffers;
using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wyoming.Net.Tts.Backend.Kokoro;

public sealed class KokoroBackend : IInferenceBackend, IAsyncDisposable
{
    public const int SampleRate = 24000;
    public const int Width = 2;
    public const int Channels = 1;
    
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly InferenceSession session;
    private readonly SessionOptions defaultOptions = new()
    {
        EnableMemoryPattern = true, 
        InterOpNumThreads = 8, 
        IntraOpNumThreads = 8
    };

    private readonly KokoroVoice voice;
    private readonly float speed;
    private const int MaxTokens = 510;

    private bool disposed;

    public KokoroBackend(string voice, float speed = 1)
    { 
        if (Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            defaultOptions.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_USE_CPU_AND_GPU);
        }
        session = new InferenceSession(CrossPlatformHelper.GetModelPath(), defaultOptions);
        this.voice = KokoroVoice.FromPath(Path.Combine(CrossPlatformHelper.GetVoicesPath(), $"{voice}.npy"));
        this.speed = speed;
    }
    
    public async Task SynthesizeAsync(string text, OnStreamAsync callback)
    {
        var tokens = await Tokenizer.TokenizeAsync(text, voice.GetLangCode());
        int windowPos = 0;
        int iteration = 0;

        // Chunk to the model max tokens
        while (windowPos < tokens.Length)
        {
            int windowSize = Math.Min(MaxTokens, tokens.Length - windowPos);

            await Infer(
                new Memory<int>(tokens, windowPos, windowSize),
                callback,
                iteration
            );

            windowPos += windowSize;
            iteration++;
        }

        await callback(Memory<float>.Empty, -1);
    }
    
    private async Task Infer(Memory<int> tokens, OnStreamAsync callback, int iteration)
    {
        const int b = 1;
        var features = voice.Features;
        var t = tokens.Length;
        var c =  features.GetLength(2);
        
        if (tokens.Length == 0) 
        {
            Debug.WriteLine("Received empty input token array. Returning");
            return;
        }

        var tokenTensor = new DenseTensor<long>([b, t + 2]); // <start>{text}<end>
        var styleTensor = new DenseTensor<float>([b, c]); // Voice features
        var speedTensor = new DenseTensor<float>(new[] { speed }, [b]);

        for (int j = 0; j < c; j++)
        {
            styleTensor[0, j] = features[t - 1, 0, j];
        }

        // BOS (implicitly 0, but explicit for clarity)
        tokenTensor[0, 0] = 0;
        
       for (int i = 0; i < t; i++) 
       {
           int token = tokens.Span[i];
           tokenTensor[0, i + 1] = token >= 0 ? token : 4; // [unk] --> '.'
       }
        
        // EOS (also 0)
        tokenTensor[0, t + 1] = 0;

        NamedOnnxValue[] inputs =
        [
            GetOnnxValue("tokens", tokenTensor),
            GetOnnxValue("style", styleTensor),
            GetOnnxValue("speed", speedTensor)
        ];

        try
        {
            await semaphore.WaitAsync();

            if (disposed)
            {
                return;
            }
            
            using var results = session.Run(inputs);
            var resultTensor = results[0].AsTensor<float>();

            if (resultTensor is DenseTensor<float> denseTensor)
            {
                await callback(denseTensor.Buffer, iteration);
                await HandlePauseAsync(tokens, callback, iteration);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task HandlePauseAsync(Memory<int> tokens, OnStreamAsync callback, int iteration)
    {
        if (Tokenizer.TryGetChar(tokens.Span[^1], out var c) && Tokenizer.IsPunctuation(c))
        {
            var secondsToPause = PauseAfterSegmentStrategy.GetPauseForSegment(c);
            var sampleLen = (int)secondsToPause * SampleRate;
            var silenceSamples = ArrayPool<float>.Shared.Rent(sampleLen);
            
            try
            {
                if (secondsToPause > 0)
                {
                    var span = silenceSamples.AsSpan().Slice(0, sampleLen);
                    
                    // Zero memory
                    span.Clear();
                    
                    await callback(new Memory<float>(silenceSamples, 0, sampleLen), iteration);
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(silenceSamples);
            }
        }
    }

    private static NamedOnnxValue GetOnnxValue<T>(string name, DenseTensor<T> val)
    {
        return NamedOnnxValue.CreateFromTensor(name, val);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await semaphore.WaitAsync();
            await CastAndDispose(session);
            await CastAndDispose(defaultOptions);
        }
        finally
        {
            disposed = true;
            semaphore.Release();
        }
        
        await CastAndDispose(semaphore);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}