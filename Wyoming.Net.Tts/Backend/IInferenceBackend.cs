namespace Wyoming.Net.Tts.Backend;

/// <summary>
/// <param name="iteration">-1 for end of iterations</param>
/// </summary>
public delegate Task OnStreamAsync(Memory<float> samples, int iteration);

public interface IInferenceBackend : IAsyncDisposable
{
    Task SynthesizeAsync(string text, OnStreamAsync callback);
}