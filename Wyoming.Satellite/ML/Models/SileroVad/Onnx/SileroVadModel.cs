using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Wyoming.Net.Core;

namespace Wyoming.Net.Satellite.ML.Models.SileroVad.Onnx;

public sealed class SileroVadModel : IDisposable
{
    private static readonly SessionOptions DefaultSessionOptions = new();

    static SileroVadModel()
    {
#if ANDROID
        DefaultSessionOptions.AppendExecutionProvider_Nnapi();
        DefaultSessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_VERBOSE;
        DefaultSessionOptions.LogVerbosityLevel = 2;
#endif
#if IOS || MACCATALYST
       DefaultSessionOptions.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_USE_CPU_AND_GPU);
#endif

        DefaultSessionOptions.InterOpNumThreads = 1;
        DefaultSessionOptions.IntraOpNumThreads = 1;
        DefaultSessionOptions.EnableCpuMemArena = true;
    }

    private const int SampleRate = 16000;
    private const int BatchSize = 512; // batch size at 16000hz
    private const int ContextSize = 64; // context size at 16000hz

    private readonly InferenceSession session;
    private readonly float[] state = new float[2 * 1 * 128];
    private readonly float[,] context;
    private readonly float[] padBuffer = new float[BatchSize];
    
    private readonly DenseTensor<float> inputTensor = new(new int[] { 1, BatchSize + ContextSize });
    private readonly DenseTensor<float> stateTensor;

    private readonly NamedOnnxValue srOnnxValue 
        = NamedOnnxValue.CreateFromTensor("sr", new DenseTensor<long>(new[] { (long)SampleRate }, new int[] {1}));

    private readonly List<NamedOnnxValue> inputs = new(3);

    public SileroVadModel(byte[] model)
    {
        session = new InferenceSession(model, DefaultSessionOptions);
        context = new float[BatchSize, ContextSize];

        for (int row = 0; row < BatchSize; row++)
        {
            for (int col = 0; col < ContextSize; col++)
            {
                context[row, col] = 0;
            }
        }
        
        stateTensor = new DenseTensor<float>(new int[] { 2, 1, 128 });
        var inputOnnValue1 = NamedOnnxValue.CreateFromTensor("input",inputTensor);
        var stateOnnxValue1 = NamedOnnxValue.CreateFromTensor("state", stateTensor);
        
        inputs.Add(inputOnnValue1);
        inputs.Add(srOnnxValue);
        inputs.Add(stateOnnxValue1);
    }
    
    ~SileroVadModel()
    {
        Dispose();
    }

    public void Dispose()
    {
        session.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void Concatenate(float[,] context, Span2D<float> samples, Span2D<float> dest)
    {
        const int rows = 1;
        const int colsA = BatchSize;
        const int colsB = ContextSize;

        for (int i = 0; i < rows; i++)
        {
            var row = dest.GetRowSpan(i);
            samples.GetRowSpan(i).CopyTo(row);

            for (int col = 0; col < colsB; col++)
            {
                dest[i, col + colsA] = context[i, col];
            }
        }
    }
    
    private void UpdateContext(Span2D<float> array)
    {
        int rows = array.Height;
        int cols = array.GetRowSpan(0).Length;

        float[,] result = context;

        for (int i = 0; i < rows; i++)
        {
            var row = array.GetRowSpan(i);

            for (int col = 0; col < ContextSize; col++)
            {
                result[i, col] = row[cols - ContextSize + col];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNumberOfPredictions(int sampleLength)
    {
        return (sampleLength + BatchSize - 1) / BatchSize;
    }
    
    public int BatchPredict(ReadOnlyMemory<float> samples, Span<float> dest)
    {
        int totalLength = samples.Length;
        int totalBatches = GetNumberOfPredictions(samples.Length);

        if (dest.Length < totalBatches)
        {
            throw new ArgumentException("dest span is too small.", nameof(dest));
        }

        int offset = 0;

        for (int batch = 0; batch < totalBatches; batch++)
        {
            int remaining = totalLength - offset;

            if (remaining >= BatchSize)
            {
                dest[batch] = Predict(samples.Slice(offset, BatchSize));
            }
            else
            {
                // Copy remaining samples into reusable pad buffer
                samples.Slice(offset, remaining).CopyTo(padBuffer);
                padBuffer.AsSpan(remaining).Clear(); // zero the unused tail

                dest[batch] = Predict(padBuffer);
            }

            offset += BatchSize;
        }

        return totalBatches;
    }

    public float Predict(ReadOnlyMemory<float> samples)
    {
        Asserts.IsTrue(samples.Length == BatchSize, "Expected sample size to equal BatchSize");
        Asserts.IsTrue(MemoryMarshal.TryGetArray(samples, out var sampleSegment) && sampleSegment.Array is not null, "Expected to be able to extract array from samples");

        unsafe
        {
            var destBuffer = stackalloc float[BatchSize + ContextSize];
            var dest = new Span2D<float>(destBuffer, 1, BatchSize + ContextSize, 0);

            Concatenate(context,
                new Span2D<float>(sampleSegment.Array!, sampleSegment.Offset, 1, sampleSegment.Count, 0), dest);

            // Fill input tensor
            dest.GetRowSpan(0).CopyTo(inputTensor.Buffer.Span);

            // Fill state tensor
            state.AsSpan().CopyTo(stateTensor.Buffer.Span);

            using var outputs = session.Run(inputs);
            var output = outputs.First(o => o.Name == "output").AsTensor<float>();
            var newState = outputs.First(o => o.Name == "stateN").AsTensor<float>();
            
            UpdateContext(dest);
            UpdateState(newState);

            return output[0];
        }
    }

    private void UpdateState(Tensor<float> newState)
    {
        int dim0 = newState.Dimensions[0];
        int dim1 = newState.Dimensions[1];
        int dim2 = newState.Dimensions[2];

        for (int i = 0; i < dim0; i++)
        {
            int baseI = i * dim1 * dim2;

            for (int j = 0; j < dim1; j++)
            {
                int baseJ = baseI + j * dim2;

                for (int k = 0; k < dim2; k++)
                {
                    state[baseJ + k] = newState[i, j, k];
                }
            }
        }
    }
}