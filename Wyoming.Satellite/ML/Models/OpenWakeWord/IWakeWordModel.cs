namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IWakeWordModel : IDisposable
{
    int FlatShapeSize { get; }
    
    float Predict(ReadOnlySpan<float> input);
}