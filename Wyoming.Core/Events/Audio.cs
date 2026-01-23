using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Core.Events;

public abstract class AudioFormat
{
    public int Rate { get; init; }

    public int Width { get; init; }

    public int Channels { get; init; }

    protected AudioFormat(int rate, int width, int channels)
    {
        Rate = rate;
        Width = width;
        Channels = channels;
    }

    protected AudioFormat() { }
}

public sealed class AudioChunk : AudioFormat, IEventable
{
    private const string ChunkType = "audio-chunk";

    public byte[] Audio { get; init; } = Array.Empty<byte>();

    public long? Timestamp { get; init; }

    public static bool IsType(string eventType) =>
        ChunkType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>
        {
            { "rate", Rate },
            { "width", Width },
            { "channels", Channels },
            { "timestamp", Timestamp ?? 0 }
        };

        return new Event(
            Type: ChunkType,
            Data: data.AsReadOnly(),
            Payload: Audio
        );
    }

    public static AudioChunk FromEvent(Event evt)
    {
        int rate = 0;
        if (evt.TryGetDataValue("rate", out var rateRaw))
        {
            rate = Convert.ToInt32(rateRaw);
        }

        int width = 0;
        if (evt.TryGetDataValue("width", out var widthRaw))
        {
            width = Convert.ToInt32(widthRaw);
        }

        int channels = 0;
        if (evt.TryGetDataValue("channels", out var channelsRaw))
        {
            channels = Convert.ToInt32(channelsRaw);
        }

        long? timestamp = null;
        if (evt.TryGetDataValue("timestamp", out var tsRaw))
        {
            if (tsRaw is long l)
            {
                timestamp = l;
            }
            else if (tsRaw is int i)
            {
                timestamp = Convert.ToInt64(i);
            }
        }

        byte[] audio = evt.Payload ?? Array.Empty<byte>();

        return new AudioChunk
        {
            Rate = rate,
            Width = width,
            Channels = channels,
            Audio = audio,
            Timestamp = timestamp
        };
    }

    public int Samples
    {
        get
        {
            if (Width == 0 || Channels == 0)
            {
                return 0;
            }

            return Audio.Length / (Width * Channels);
        }
    }

    public double Seconds
    {
        get
        {
            if (Rate == 0)
            {
                return 0.0;
            }

            return (double)Samples / Rate;
        }
    }

    public int Milliseconds => (int)(Seconds * 1_000.0);

    public static AudioChunk FromFloatPcm(byte[] samplesPcmFloat, long? timestamp, int rate, int channels)
    {
        return FromFloatPcm(samplesPcmFloat.AsSpan(), timestamp, rate, channels);
    }
    
    public static AudioChunk FromFloatPcm(ReadOnlySpan<byte> samplesPcmFloat, long? timestamp, int rate, int channels)
    {
        AudioChunk chunk = new()
        {
            Audio = AudioOp.FloatToPcm16(samplesPcmFloat),
            Timestamp = timestamp,
            Rate = rate,
            Channels = channels,
            Width = sizeof(short)
        };

        return chunk;
    }
}

public sealed class AudioStart : AudioFormat, IEventable
{
    private const string StartType = "audio-start";

    public long? Timestamp { get; init; }

    public static bool IsType(string eventType) =>
        StartType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>
        {
            { "rate", Rate },
            { "width", Width },
            { "channels", Channels },
        };

        if(Timestamp.HasValue)
        {
            data["timestamp"] = Timestamp.Value;
        }

        return new Event(
            StartType,
            data.AsReadOnly()
        );
    }

    public static AudioStart FromEvent(Event evt)
    {

        int rate = 0;
        if (evt.TryGetDataValue("rate", out var rateRaw))
        {
            rate = Convert.ToInt32(rateRaw);
        }

        int width = 0;
        if (evt.TryGetDataValue("width", out var widthRaw))
        {
            width = Convert.ToInt32(widthRaw);
        }

        int channels = 0;
        if (evt.TryGetDataValue("channels", out var chRaw))
        {
            channels = Convert.ToInt32(chRaw);
        }

        long? timestamp = null;
        if (evt.TryGetDataValue("timestamp", out var tsRaw))
        {
            if (tsRaw is long l)
            {
                timestamp = l;
            }
            else if (tsRaw is int i)
            {
                timestamp = i;
            }
        }

        return new AudioStart
        {
            Rate = rate,
            Width = width,
            Channels = channels,
            Timestamp = timestamp
        };
    }
}

public sealed class AudioStop : IEventable
{
    private const string StopType = "audio-stop";

    public long? Timestamp { get; init; }

    public static bool IsType(string eventType) =>
        StopType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = Timestamp.HasValue ? new Dictionary<string, object>
        {
            { "timestamp", Timestamp }
        } : null;

        return new Event(
            Type: StopType,
            Data: data?.AsReadOnly()
        );
    }

    public static AudioStop FromEvent(Event evt)
    {
        long? timestamp = null;

        if (evt.TryGetDataValue("timestamp", out var tsRaw))
        {
            if (tsRaw is long l)
            {
                timestamp = l;
            }
            else if (tsRaw is int i)
            {
                timestamp = Convert.ToInt64(i);
            }
            else if(tsRaw is not null)
            {
                timestamp = (long)Convert.ChangeType(tsRaw, TypeCode.Int64);
            }
        }

        return new AudioStop
        {
            Timestamp = timestamp
        };
    }
}