#if TIZEN8_0_OR_GREATER
using System.Runtime.InteropServices;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Tizen;

public sealed class EmbeddingModel : TizenModel, IEmbeddingModel
{
    public EmbeddingModel(string modelPath) : base(modelPath)
    {
    }

    public int FlatShapeSize => 1 * 76 * 32 * 1;

    public int FlattenedOutputSize => 96;

    public void GenerateAudioEmbeddings(ReadOnlySpan<float> input, Span<float> destination)
    {
        using var tensorData = engine.Input.GetTensorsData();
        var bytes = MemoryMarshal.Cast<float, byte>(input);

        // TODO: add array pooling
        tensorData.SetTensorData(0, bytes.ToArray());

        using var outData = engine.Invoke(tensorData);
        var bytesOut = outData.GetTensorData(0);

        MemoryMarshal.Cast<byte, float>(bytesOut).CopyTo(destination);
    }
}

#endif