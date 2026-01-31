using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

public sealed class EmbeddingModel : BaseModel
{
    public EmbeddingModel(byte[] model) : base(model)
    {
    }

    private static readonly long[] Shape = {1, 76, 32, 1};
    public const int FlatShapeSize = 1 * 76 * 32 * 1;

    internal ModelOutput GenerateAudioEmbeddings(in ReadOnlySpan<float> input)
    {
        using var ortTensor = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, Shape);

        var tensorValue = ortTensor.GetTensorMutableDataAsSpan<float>();
        input.CopyTo(tensorValue);

        var modelInput = new ModelInput("input_1", ortTensor);

        var result = session.Run(DefaultRunOptions, modelInput, session.OutputNames);

        return new ModelOutput(result);
    }
}