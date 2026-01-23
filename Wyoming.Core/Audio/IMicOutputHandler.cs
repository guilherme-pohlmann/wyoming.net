namespace Wyoming.Net.Core.Audio;

public interface IMicOutputHandler
{
    ValueTask OnMicAudioAsync(byte[] buffer, long? timestamp);
}
