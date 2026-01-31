using System.Collections.ObjectModel;

namespace Wyoming.Net.Core.Events;

public sealed class Describe : IEventable
{
    public const string Type = "describe";

    public static IEventable FromEvent(Event @event) => new Describe();

    public static bool IsType(string type) => type.Equals(Type, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent() => new(Type);
}

public sealed class Info : IEventable
{
    public Info(Satellite? satellite)
    {
        Satellite = satellite;
    }

    public const string Type = "info";

    /// <summary>
    /// Satellite information
    /// </summary>
    public Satellite? Satellite { get; init; }
    
    public IEnumerable<TtsProgram> Tts { get; init; } = Array.Empty<TtsProgram>();

    public static IEventable FromEvent(Event @event)
    {
        Satellite? satellite = null;

        if (@event.TryGetDataValue("satellite", out var rawObj) && rawObj is IDictionary<string, object> dict)
        {
            satellite = Satellite.FromDict(dict);
        }

        return new Info(satellite);
    }

    public static bool IsType(string type) => type.Equals(Type, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>()
        {
            { "tts", Tts.Select(it => it.ToDict()) }
        };

        if (Satellite is not null)
        {
            data["satellite"] = Satellite.ToDict();
        }

        return new Event(Type, data.AsReadOnly());
    }
}

public sealed class Attribution
{
    /// <summary>
    /// Who made it.
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Where it's from.
    /// </summary>
    public string Url { get; init; } = default!;

    public ReadOnlyDictionary<string, string?> ToDict()
    {
        var data = new Dictionary<string, string?>
        {
            { "name", Name },
            { "url", Url },
        };

        return data.AsReadOnly();
    }
}

public class Artifact
{
    /// <summary>
    /// Name/id of artifact.
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Who made the artifact and where it's from.
    /// </summary>
    public Attribution Attribution { get; init; } = default!;

    /// <summary>
    /// True if the artifact is currently installed.
    /// </summary>
    public bool Installed { get; init; }

    /// <summary>
    /// Human-readable description of the artifact.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version of the artifact.
    /// </summary>
    public string? Version { get; init; }
}


public sealed class Satellite : Artifact
{
    /// <summary>Name of the area the satellite is in.</summary>
    public string? Area { get; init; }

    /// <summary>True if a local VAD will be used to detect the end of voice commands.</summary>
    public bool HasVad { get; init; }

    /// <summary>Wake words that are currently being listened for.</summary>
    public IEnumerable<string>? ActiveWakeWords { get; init; }

    /// <summary>Maximum number of local wake words that can be run simultaneously.</summary>
    public int? MaxActiveWakeWords { get; init; }

    /// <summary>Satellite supports remotely triggering pipeline runs.</summary>
    public bool? SupportsTrigger { get; init; }

    public Dictionary<string, object> ToDict()
    {
        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(Name))
        {
            data["name"] = Name;
        }

        if (!string.IsNullOrEmpty(Area))
        {
            data["area"] = Area;
        }

        if(!string.IsNullOrEmpty(Description))
        {
            data["description"] = Description;
        }

        if (!string.IsNullOrEmpty(Version))
        {
            data["version"] = Version;
        }

        if (ActiveWakeWords is not null)
        {
            data["active_wake_words"] = ActiveWakeWords;
        }

        if (MaxActiveWakeWords is not null)
        {
            data["max_active_wake_words"] = MaxActiveWakeWords.Value;
        }

        if (SupportsTrigger is not null)
        {
            data["supports_trigger"] = SupportsTrigger.Value;
        }

        if(Attribution is not null)
        {
            data["attribution"] = Attribution.ToDict();
        }

        data["installed"] = Installed;
        data["has_vad"] = HasVad;

        return data;
    }

    public static Satellite? FromDict(IDictionary<string, object> dict)
    {
        if (dict is null)
        {
            return null;
        }

        dict.TryGetValue("area", out var areaRaw);
        var area = areaRaw as string;

        bool hasVad = false;
        if (dict.TryGetValue("has_vad", out var vadRaw) && vadRaw is bool b1)
            hasVad = b1;

        IEnumerable<string>? activeWakeWords = null;

        if (dict.TryGetValue("active_wake_words", out var wakeRaw))
        {
            if (wakeRaw is IEnumerable<object> objList)
            {
                activeWakeWords = objList.Select(o => o?.ToString() ?? string.Empty);
            }
            else if (wakeRaw is IEnumerable<string> strList)
            {
                activeWakeWords = strList;
            }
        }

        int? maxActive = null;

        if (dict.TryGetValue("max_active_wake_words", out var maxRaw))
        {
            if (maxRaw is int i)
            {
                maxActive = i;
            }
            else if (maxRaw is long l)
            {
                maxActive = (int)l;
            }
        }

        bool? supportsTrigger = null;

        if (dict.TryGetValue("supports_trigger", out var triggerRaw) && triggerRaw is bool b2)
        {
            supportsTrigger = b2;
        }

        dict.TryGetValue("name", out var nameRaw);
        var name = nameRaw as string ?? "";

        return new Satellite
        {
            Name = name,
            Area = area,
            HasVad = hasVad,
            ActiveWakeWords = activeWakeWords,
            MaxActiveWakeWords = maxActive,
            SupportsTrigger = supportsTrigger
        };
    }
}

public sealed class TtsVoiceSpeaker
{
    public string Name { get; init; } = null!;
    
    public IReadOnlyDictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>()
        {
            { "name", Name },
        };
    }
}

public sealed class TtsVoice : Artifact
{
    public IEnumerable<string> Languages { get; init; } = Array.Empty<string>();
    
    public IEnumerable<TtsVoiceSpeaker> Speakers { get; init; } = Array.Empty<TtsVoiceSpeaker>();

    public IReadOnlyDictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>()
        {
            { "name", Name },
            { "attribution", Attribution.ToDict() },
            { "installed", Installed },
            { "version", Version ?? string.Empty },
            { "description", Description ?? string.Empty },
            { "languages", Languages },
            { "speakers", Speakers.Select(it => it.ToDict()) },
        };
    }
}

public sealed class TtsProgram : Artifact
{
    public IEnumerable<TtsVoice> Voices { get; init; } = Array.Empty<TtsVoice>();
    
    public bool SupportsSynthesizeStreaming {get; init; }

    public IReadOnlyDictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>()
        {
            { "name", Name },
            { "attribution", Attribution.ToDict() },
            { "installed", Installed },
            { "version", Version ?? string.Empty },
            { "description", Description ?? string.Empty },
            { "voices", Voices.Select(it => it.ToDict()) },
            { "supports_synthesize_streaming", SupportsSynthesizeStreaming },
        };
    }
}
