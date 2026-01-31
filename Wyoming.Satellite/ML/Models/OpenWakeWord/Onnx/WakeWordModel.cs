using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

public sealed class WakeWordModel : BaseModel
{
    public WakeWordModel(byte[] model) : base(model)
    {
    }

    private static readonly long[] Shape = { 1, 16, 96 };
    public const int FlatShapeSize = 1 * 16 * 96;

    internal float Predict(ReadOnlySpan<float> input)
    {
        using var ortTensor = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, Shape);
        
        var tensorValue = ortTensor.GetTensorMutableDataAsSpan<float>();
        input.CopyTo(tensorValue);

        var modelInput = new ModelInput("onnx::Flatten_0", ortTensor);

        using var result = session.Run(DefaultRunOptions, modelInput, session.OutputNames);

        var output = result[0];
        var tensorData = output.GetTensorDataAsSpan<float>();

        return tensorData[0];
    }
}