using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Tts.KokoroBackend;

public sealed class KokoroBackend : ITextToSpeechProvider
{
    public static class Constants
    {
        public const int SampleRate = 24000;

        public const int ChannelCount = 1;

        public const int Width = 2;
        
        public const int MaxTokens = 510;
    }
    
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly SessionOptions defaultOptions = new()
    {
        EnableMemoryPattern = true, 
        InterOpNumThreads = 8, 
        IntraOpNumThreads = 8
    };

    
    private readonly float speed;
    private readonly bool useCuda;

    private InferenceSession session;
    private KokoroVoice kokoroVoice;
    private bool disposed;

    public KokoroBackend(float speed = 1, bool useCuda = false)
    {
        this.speed = speed;
        this.useCuda = useCuda;
    }

    public int SampleRate => Constants.SampleRate;

    public int ChannelCount => Constants.ChannelCount;

    public int Width => Constants.Width;

    public async Task SynthesizeAsync(string text, OnStreamAsync callback)
    {
        var tokens = await Tokenizer.TokenizeAsync(text, kokoroVoice.GetLangCode());
        int iteration = 0;
        
        foreach (var segment in SegmentationStrategy.SplitToSegments(tokens))
        {
            await Infer(
                segment,
                callback,
                iteration
            );
            
            iteration++;
        }

        await callback(Memory<float>.Empty, -1);
    }

    public async Task InitializeAsync(string model, string voice)
    {
        var loadedModel = await ModelManager.LoadModelAsync(model);
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Current implementation is not suitable for CoreML, but leaving this here anyway
            // Maybe someone can make it coreml friendly
            defaultOptions.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH | CoreMLFlags.COREML_FLAG_USE_CPU_AND_GPU);
            
        }
        else if (useCuda)
        {
            defaultOptions.AppendExecutionProvider_CUDA();
        }
        else
        {
            defaultOptions.AppendExecutionProvider_CPU();
        }
        
        defaultOptions.InterOpNumThreads = 1;
        defaultOptions.IntraOpNumThreads = Environment.ProcessorCount;
        defaultOptions.EnableMemoryPattern = true;

        session = new InferenceSession(loadedModel, defaultOptions);
        kokoroVoice = KokoroVoice.FromPath(Path.Combine(CrossPlatformHelper.GetVoicesPath(), $"{voice}.npy"));
    }

    private async Task Infer(Memory<int> tokens, OnStreamAsync callback, int iteration)
    {
        const int b = 1;
        var features = kokoroVoice.Features;
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

    private async Task HandlePauseAsync(Memory<int> tokens, OnStreamAsync callback, int iteration)
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