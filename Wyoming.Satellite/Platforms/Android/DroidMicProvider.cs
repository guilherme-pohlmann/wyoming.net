using Android.Media;
using System.Buffers;
using System.Runtime.InteropServices;
using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Satellite;

public sealed class DroidMicProvider : IMicInputProvider
{
    private readonly AudioRecord audioRecorder;

    public DroidMicProvider()
    {
        //TODO: is this good buffer size?
        audioRecorder = new AudioRecord(AudioSource.VoiceRecognition, Rate, ChannelIn.Mono, Encoding.PcmFloat, 1280 * Width * 8);
    }

    public int Rate => 16000;

    public int Channels => 1;

    public int Width => sizeof(float);

    public void Dispose()
    {
        StopRecording();
        audioRecorder.Release();
        audioRecorder.Dispose();
    }

    public async Task<long?> ReadAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        int sampleSizeFloat = buffer.Length / Width;
        float[] samples = ArrayPool<float>.Shared.Rent(sampleSizeFloat);

        try
        {
            if(cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            const int readBlockingMode = 0;
            int read = await audioRecorder.ReadAsync(samples, 0, sampleSizeFloat, readBlockingMode).WaitAsync(cancellationToken);

            var bytes = MemoryMarshal.AsBytes(samples.AsSpan().Slice(0, read));
            bytes.CopyTo(buffer);
            
            return TryGetTimestamp();
        }
        finally
        {
            ArrayPool<float>.Shared.Return(samples);
        }
    }

    public async ValueTask StartRecordingAsync()
    {
        var hasPermission = await EnsureMicrophonePermissionAsync();

        if (!hasPermission)
        {
            return;
        }
        audioRecorder.StartRecording();
    }

    public ValueTask StopRecordingAsync()
    {
        StopRecording();
        return ValueTask.CompletedTask;
    }

    private void StopRecording()
    {
        audioRecorder.Stop();
    }
    
    private long? TryGetTimestamp()
    {
        var audioTimestamp = new AudioTimestamp();

        if (audioRecorder.GetTimestamp(audioTimestamp, AudioTimebase.Monotonic) == 0)
        {
            long framePosition = audioTimestamp.FramePosition; // frames since start
            //long nanoTime = audioTimestamp.NanoTime;           // ns, monotonic clock

            //long timestampMs = nanoTime / 1_000_000L;

            return Convert.ToInt64(framePosition * 1000.0 / Rate);
        }

        return null;
    }

    private static async Task<bool> EnsureMicrophonePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status == PermissionStatus.Granted)
            return true;

        if (Permissions.ShouldShowRationale<Permissions.Microphone>())
        {
            //TODO:
            //await DisplayAlert(
            //    "Microphone permission",
            //    "This app needs access to the microphone to record audio.",
            //    "OK");
        }

        status = await Permissions.RequestAsync<Permissions.Microphone>();

        return status == PermissionStatus.Granted;
    }
}
