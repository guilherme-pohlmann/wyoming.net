namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IMelspectrogramModel : IDisposable
{
    int FlattenedOutputSize { get; }
    
    void GenerateSpectrogram(ReadOnlySpan<float> input, Span<float> destination);
}