using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;
using Wyoming.Net.Core.Audio;
using Wyoming.Net.Satellite.App.Tz.Platform.Interop;

namespace Wyoming.Net.Satellite.App.Tz.Platform;

internal sealed class TizenMicProvider : IMicInputProvider
{
    private readonly AudioCapture audioCapture;
    private readonly IntPtr audioCaptureHandle;

    public TizenMicProvider()
    {
        audioCapture = new AudioCapture(Rate, AudioChannel.Mono, AudioSampleType.S16Le);
        audioCaptureHandle = (IntPtr)audioCapture.GetType()
                                                 .GetField("_handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                                 .GetValue(audioCapture);
    }

    public int Rate => 16000;

    public int Channels => 1;

    public int Width => sizeof(float);

    public Task<long?> ReadAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var sampleCount = buffer.Length / Width;
        Span<byte> audio = stackalloc byte[sampleCount * sizeof(short)];

        NativeAudio.Read(audioCaptureHandle, ref MemoryMarshal.GetReference(audio), audio.Length).ThrowIfFailed("Failed to read audio");

        AudioOp.Pcm16ToFloat(audio, MemoryMarshal.Cast<byte, float>(buffer));
        return Task.FromResult<long?>(null);
    }

    public ValueTask StartRecordingAsync()
    {
        audioCapture.Prepare();
        audioCapture.Resume();

        return ValueTask.CompletedTask;
    }

    public ValueTask StopRecordingAsync()
    {
        audioCapture.Pause();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        audioCapture.Dispose();
    }
}