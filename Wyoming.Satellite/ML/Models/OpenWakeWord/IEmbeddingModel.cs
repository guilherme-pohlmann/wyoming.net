namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IEmbeddingModel : IDisposable
{
    int FlatShapeSize { get; }
    
    int FlattenedOutputSize { get; }
    
    void GenerateAudioEmbeddings(ReadOnlySpan<float> input, Span<float> destination);
}