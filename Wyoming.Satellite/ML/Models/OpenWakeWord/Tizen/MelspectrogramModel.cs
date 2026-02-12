#if TIZEN8_0_OR_GREATER

using System.Runtime.InteropServices;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Tizen;

public sealed class MelspectrogramModel : TizenModel, IMelspectrogramModel
{
    public MelspectrogramModel(string modelPath) : base(modelPath)
    {
    }

    public int FlattenedOutputSize => 256;

    public void GenerateSpectrogram(ReadOnlySpan<float> input, Span<float> destination)
    {
        using var tensorData = engine.Input.GetTensorsData();
        var bytes = MemoryMarshal.Cast<float, byte>(input);

        // TODO: add array pooling
        tensorData.SetTensorData(0, bytes.ToArray());

        using var outData = engine.Invoke(tensorData);
        var bytesOut = outData.GetTensorData(0);

        MemoryMarshal.Cast<byte, float>(bytesOut).CopyTo(destination);
        Normalize(destination);
    }

    private static void Normalize(Span<float> outputBuffer)
    {
        for (int i = 0; i < outputBuffer.Length; i++)
        {
            outputBuffer[i] = outputBuffer[i] / 10.0f + 2.0f;
        }
    }
}

#endif