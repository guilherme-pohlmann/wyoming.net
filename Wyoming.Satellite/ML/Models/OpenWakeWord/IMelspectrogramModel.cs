namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IMelspectrogramModel : IDisposable
{
    int FlattenedOutputSize { get; }
    
    void GenerateSpectrogram(in ReadOnlySpan<float> input, in Span<float> destination);
}