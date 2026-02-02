#if NET9_0_OR_GREATER

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

public sealed class EmbeddingModel : BaseModel, IEmbeddingModel
{
    public EmbeddingModel(byte[] model) : base(model)
    {
    }

    private static readonly long[] Shape = [ 1, 76, 32, 1 ];
    
    public int FlatShapeSize => 1 * 76 * 32 * 1;

    public int FlattenedOutputSize => 96;

    public void GenerateAudioEmbeddings(in ReadOnlySpan<float> input, in Span<float> destination)
    {
        using var ortTensor = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, Shape);

        var tensorValue = ortTensor.GetTensorMutableDataAsSpan<float>();
        input.CopyTo(tensorValue);

        var modelInput = new ModelInput("input_1", ortTensor);

        var result = session.Run(DefaultRunOptions, modelInput, session.OutputNames);
        using var modelOutput = new ModelOutput(result);
        
        modelOutput.FlattenTo(destination);
    }
}
#endif