

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

#if NET9_0_OR_GREATER

using Microsoft.ML.OnnxRuntime;
using System.Numerics.Tensors;

#pragma warning disable SYSLIB5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public delegate void SampleTransformer(Span<float> buffer);


public readonly ref struct ModelOutput : IDisposable
{
    private readonly IDisposableReadOnlyCollection<OrtValue> result;

    private readonly ReadOnlyTensorSpan<float> tensor;

    private readonly SampleTransformer? transformer;

    public ModelOutput(IDisposableReadOnlyCollection<OrtValue> result, SampleTransformer? transformer = null)
    {
        this.result = result;
        tensor = result[0].GetTensorDataAsTensorSpan<float>();
        this.transformer = transformer;
    }

    public int FlattenedLength => (int)tensor.FlattenedLength;

    public void FlattenTo(in Span<float> buffer)
    {
        var tensorData = tensor.Squeeze();
        tensorData.FlattenTo(buffer);
        transformer?.Invoke(buffer);
    }

    public void Dispose()
    {
        result.Dispose();
    }
}
#pragma warning restore SYSLIB5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#else

public readonly struct ModelOutput : IDisposable
{
    public int FlattenedLength => throw new NotImplementedException();

    public void FlattenTo(in Span<float> buffer)
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

#endif