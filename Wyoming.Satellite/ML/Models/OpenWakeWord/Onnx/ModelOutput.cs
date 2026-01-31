#if NET9_0_OR_GREATER

using Microsoft.ML.OnnxRuntime;
using System.Numerics.Tensors;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

#pragma warning disable SYSLIB5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal readonly ref struct ModelOutput : IDisposable
{
    private readonly IDisposableReadOnlyCollection<OrtValue> result;

    private readonly ReadOnlyTensorSpan<float> tensor;

    private readonly Action<Span<float>>? transformer;

    public ModelOutput(IDisposableReadOnlyCollection<OrtValue> result, Action<Span<float>>? transformer = null)
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
namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;
// Just a dummy class to satisfy the compiler and make things simpler
// But bottom line is we just support net6 because of Tizen but Tizen doesn't run onnx anyway
internal readonly struct ModelOutput : IDisposable
{
    public ModelOutput(object result, object? transformer = null)
    {
        throw new PlatformNotSupportedException();
    }

    public int FlattenedLength => throw new PlatformNotSupportedException();

    public void FlattenTo(in Span<float> buffer)
    {
        throw new PlatformNotSupportedException();
    }

    public void Dispose()
    {
        throw new PlatformNotSupportedException();
    }
}
#endif