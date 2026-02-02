namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IEmbeddingModel : IDisposable
{
    int FlatShapeSize { get; }
    
    int FlattenedOutputSize { get; }
    
    void GenerateAudioEmbeddings(in ReadOnlySpan<float> input, in Span<float> destination);
}