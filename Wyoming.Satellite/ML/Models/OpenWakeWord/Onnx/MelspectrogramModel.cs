using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

public sealed class MelspectrogramModel(byte[] model) : BaseModel(model)
{
    private static readonly long[] Shape = [1, 1760];//1280];

    internal ModelOutput GenerateSpectogram(in ReadOnlySpan<float> input)
    {
        Span<float> scaled = stackalloc float[input.Length];
        input.CopyTo(scaled);

        Scale(scaled);

        using var ortTensor = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, Shape);

        var tensorValue = ortTensor.GetTensorMutableDataAsSpan<float>();
        input.CopyTo(tensorValue);

        var modelInput = new ModelInput("input", ortTensor);

        var result = session.Run(DefaultRunOptions, modelInput, session.OutputNames);

        return new ModelOutput(result, Normalize);
    }

    private static void Normalize(Span<float> outputBuffer)
    {
        for (int i = 0; i < outputBuffer.Length; i++)
            outputBuffer[i] = outputBuffer[i] / 10.0f + 2.0f;
    }

    private static void Scale(Span<float> input)
    {
        const float scale = 1.0f / 32768.0f;

        for (int i = 0; i < input.Length; i++)
        {
            input[i] = input[i] * scale;
        }
    }
}