using Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IEmbeddingModel : IDisposable
{
    int FlatShapeSize { get; }
    
    ModelOutput GenerateAudioEmbeddings(in ReadOnlySpan<float> input);
}