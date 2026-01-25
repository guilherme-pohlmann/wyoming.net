namespace Wyoming.Net.Core.Audio;

/// <summary>
/// <param name="iteration">-1 for end of iterations</param>
/// </summary>
public delegate Task OnStreamAsync(Memory<float> samples, int iteration);

public interface ITextToSpeechProvider : IAsyncDisposable
{
    int SampleRate { get; }
    
    int ChannelCount { get; }
    
    int Width { get; }
    
    Task SynthesizeAsync(string text, OnStreamAsync callback);

    Task InitializeAsync(string model, string voice);
}