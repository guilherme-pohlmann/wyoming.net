namespace Wyoming.Net.Core.Audio;

public interface ISpeakerProvider
{
    int? Rate { get; }

    int? Width { get; }

    int? Channels { get; }

    ValueTask StartAsync(int rate, int width, int channels);

    ValueTask StopAsync();

    Task PlayAsync(byte[] samples, long? timestamp);
}
