using Microsoft.Extensions.Logging;
using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Satellite;

public sealed class MacSoundProvider : IMicInputProvider, ISpeakerProvider
{
    private readonly Apple.AppleSoundProvider provider;
    private readonly IMicInputProvider micInputProvider;
    private readonly ISpeakerProvider speakerProvider;

    public MacSoundProvider(ILogger<MacSoundProvider> logger)
    {
        provider = new Apple.AppleSoundProvider(logger);
        micInputProvider = provider;
        speakerProvider = provider;
    }
    
    public void Dispose()
    {
        provider.Dispose();
    }
    
    int IMicInputProvider.Rate => micInputProvider.Rate;
    
    int IMicInputProvider.Channels => micInputProvider.Channels;

    int IMicInputProvider.Width => micInputProvider.Width;
    
    ValueTask IMicInputProvider.StartRecordingAsync()
    {
        return micInputProvider.StartRecordingAsync();
    }

    ValueTask IMicInputProvider.StopRecordingAsync()
    {
        return micInputProvider.StartRecordingAsync();
    }

    Task<long?> IMicInputProvider.ReadAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        return micInputProvider.ReadAsync(buffer, cancellationToken);
    }

    int? ISpeakerProvider.Rate => speakerProvider.Rate;

    int? ISpeakerProvider.Width => speakerProvider.Width;

    int? ISpeakerProvider.Channels => speakerProvider.Channels;

    ValueTask ISpeakerProvider.StartAsync(int rate, int width, int channels)
    {
        return speakerProvider.StartAsync(rate, width, channels);
    }

    ValueTask ISpeakerProvider.StopAsync()
    {
        return speakerProvider.StopAsync();
    }

    Task ISpeakerProvider.PlayAsync(byte[] samples, long? timestamp)
    {
        return  speakerProvider.PlayAsync(samples, timestamp);
    }
}
