using Android.Media;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;
using Encoding =  Android.Media.Encoding;

namespace Wyoming.Net.Satellite;

public sealed class DroidSpeakerProvider : ISpeakerProvider
{
    private AudioTrack? track;

    public int? Rate { get; private set; }

    public int? Width { get; private set; }

    public int? Channels { get; private set; }

    public Task PlayAsync(byte[] samples, long? timestamp)
    {
        Asserts.IsNotNull(track, "StartAsync should have been called at this point");
        return track!.WriteAsync(samples, 0, samples.Length, WriteMode.Blocking) ?? Task.CompletedTask;
    }

    public ValueTask StartAsync(int sampleRate, int width, int channels)
    {
        if(track is not null)
        {
            return ValueTask.CompletedTask;
        }

        ChannelOut channelOut = channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
        Encoding encoding = GetEncoding(width);

        var audioFormat = new AudioFormat.Builder()
            .SetSampleRate(sampleRate)!
            .SetEncoding(encoding)!
            .SetChannelMask(channelOut)
            .Build();

        var audioAttributes = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build();

        int minBuffer = AudioTrack.GetMinBufferSize(sampleRate, channelOut, encoding);

        track = new AudioTrack.Builder()
            .SetAudioAttributes(audioAttributes!)
            .SetAudioFormat(audioFormat!)
            .SetBufferSizeInBytes(minBuffer)
            .SetTransferMode(AudioTrackMode.Stream)
            .Build();

        track.Play();

        Rate = sampleRate;
        Width = width;
        Channels = channels;

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync()
    {
        track?.Stop();
        track?.Dispose();
        track = null;

        Rate = null;
        Width = null;
        Channels = null;

        return ValueTask.CompletedTask;
    }

    private static Encoding GetEncoding(int width)
    {
        return width switch
        {
            1 => Encoding.Pcm8bit,
            2 => Encoding.Pcm16bit,
            4 => Encoding.PcmFloat,
            _ => throw new ArgumentException($"Unsupported width: {width}")
        };
    }
}
