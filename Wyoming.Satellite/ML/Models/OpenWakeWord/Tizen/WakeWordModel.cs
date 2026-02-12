#if TIZEN8_0_OR_GREATER
using System.Runtime.InteropServices;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Tizen;

public sealed class WakeWordModel : TizenModel, IWakeWordModel
{
    public WakeWordModel(string modelPath) : base(modelPath)
    {
    }

    public int FlatShapeSize => 1 * 16 * 96;

    public float Predict(ReadOnlySpan<float> input)
    {
        using var tensorData = engine.Input.GetTensorsData();
        var bytes = MemoryMarshal.Cast<float, byte>(input);

        // TODO: add array pooling
        tensorData.SetTensorData(0, bytes.ToArray());

        using var outData = engine.Invoke(tensorData);
        var bytesOut = outData.GetTensorData(0);

        return BitConverter.ToSingle(bytesOut);
    }
}

#endif