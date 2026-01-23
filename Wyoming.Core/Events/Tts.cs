namespace Wyoming.Net.Core.Events;

public sealed class SynthesizeVoice
{
    public string? Name { get; init; }
    
    public string? Language { get; init; }
    
    public string? Speaker { get; init; }

    public IReadOnlyDictionary<string, string?> ToDict()
    {
        return new Dictionary<string, string?>
        {
            { "name", Name },
            { "language", Language },
            { "speaker", Speaker }
        }.AsReadOnly();
    }

    public static SynthesizeVoice? FromDict(IReadOnlyDictionary<string, object?>? dict)
    {
        if (dict is null)
        {
            return null;
        }
        
        return new SynthesizeVoice()
        {
            Name = dict.GetValueOrDefault("name") as string,
            Language = dict.GetValueOrDefault("language") as string,
            Speaker = dict.GetValueOrDefault("speaker") as string,
        };
    }
}

public sealed class Synthesize : IEventable
{
    private const string SynthesizeType = "synthesize";
    
    public string? Text { get; init; }
    
    public SynthesizeVoice? Voice { get; init; }
    
    public Dictionary<string, object>? Context { get; init; }
    
    public static bool IsType(string eventType) =>
        SynthesizeType.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    
    public Event ToEvent()
    {
        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(Text))
        {
            data.Add("text", Text);
        }

        if (Voice is not null)
        {
            data.Add("voice", Voice);
        }

        if (Context is not null)
        {
            data.Add("context", Context);
        }

        return new Event(SynthesizeType, data.AsReadOnly());
    }

    public static Synthesize FromEvent(Event ev)
    {
        return new Synthesize
        {
            Text = ev.Data?.GetValueOrDefault("text") as string,
            Voice = SynthesizeVoice.FromDict(
                ev.Data?.GetValueOrDefault("voice") as IReadOnlyDictionary<string, object?>),
            Context = ev.Data?.GetValueOrDefault("context") as Dictionary<string, object>
        };
    }
}

public sealed class SynthesizeStart : IEventable
{
    private const string SynthesizeStartType = "synthesize-start";
    
    public SynthesizeVoice? Voice { get; init; }
    
    public Dictionary<string, object>? Context { get; init; }
    
    public static bool IsType(string eventType) =>
        SynthesizeStartType.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    
    public Event ToEvent()
    {
        var data = new Dictionary<string, object>();

        if (Voice is not null)
        {
            data.Add("voice", Voice);
        }

        if (Context is not null)
        {
            data.Add("context", Context);
        }

        return new Event(SynthesizeStartType, data.AsReadOnly());
    }

    public static SynthesizeStart FromEvent(Event ev)
    {
        return new SynthesizeStart
        {
            Voice = SynthesizeVoice.FromDict(
                ev.Data?.GetValueOrDefault("voice") as IReadOnlyDictionary<string, object?>),
            Context = ev.Data?.GetValueOrDefault("context") as Dictionary<string, object>
        };
    }
}

public sealed class SynthesizeChunk : IEventable
{
    private const string SynthesizeChunkType = "synthesize-chunk";
    
    public string? Text { get; init; }
    
    public static bool IsType(string eventType) =>
        SynthesizeChunkType.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    
    public Event ToEvent()
    {
        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(Text))
        {
            data.Add("text", Text);
        }

        return new Event(SynthesizeChunkType, data.AsReadOnly());
    }

    public static SynthesizeChunk FromEvent(Event ev)
    {
        return new SynthesizeChunk
        {
            Text = ev.Data?.GetValueOrDefault("text") as string
        };
    }
}

public sealed class SynthesizeStop : IEventable
{
    private const string SynthesizeStopType = "synthesize-stop";
    
    public static bool IsType(string eventType) =>
        SynthesizeStopType.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    
    public Event ToEvent()
    {
        return new Event(SynthesizeStopType);
    }

    public static SynthesizeStop FromEvent(Event ev)
    {
        return new SynthesizeStop();
    }
}

public sealed class SynthesizeStopped : IEventable
{
    private const string SynthesizeStoppedType = "synthesize-stopped";
    
    public static bool IsType(string eventType) =>
        SynthesizeStoppedType.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    
    public Event ToEvent()
    {
        return new Event(SynthesizeStoppedType);
    }

    public static SynthesizeStopped FromEvent(Event ev)
    {
        return new SynthesizeStopped();
    }
}