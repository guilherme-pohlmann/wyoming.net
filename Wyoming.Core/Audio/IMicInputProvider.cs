namespace Wyoming.Net.Core.Audio;

public interface IMicInputProvider : IDisposable
{
    int Rate => 16000;

    int Channels => 1;

    int Width => sizeof(float);

    ValueTask StartRecordingAsync();

    ValueTask StopRecordingAsync();

    Task<long?> ReadAsync(byte[] buffer, CancellationToken cancellationToken);
}