using Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord;

public interface IMelspectrogramModel : IDisposable
{
    ModelOutput GenerateSpectrogram(in ReadOnlySpan<float> input);
}