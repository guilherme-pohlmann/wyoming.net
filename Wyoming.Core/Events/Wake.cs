namespace Wyoming.Net.Core.Events;


public sealed class Detection : IEventable
{
    private const string DetectionType = "detection";

    private static readonly Event CachedDetectionEvent = new(DetectionType);

    public string? Name { get; init; }

    public long? Timestamp { get; init; }

    public string? Speaker { get; init; }

    public Dictionary<string, object?>? Context { get; init; }

    public static bool IsType(string eventType) => DetectionType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        Dictionary<string, object>? data = null;

        if (!string.IsNullOrEmpty(Name))
        {
            data ??= new Dictionary<string, object>();
            data["name"] = Name;
        }

        if (Timestamp is not null)
        {
            data ??= new Dictionary<string, object>();
            data["timestamp"] = Timestamp;
        }

        if (!string.IsNullOrEmpty(Speaker))
        {
            data ??= new Dictionary<string, object>();
            data["speaker"] = Speaker;
        }

        if (Context != null)
        {
            data ??= new Dictionary<string, object>();
            data["context"] = Context;
        }

        if (data is null)
        {
            return CachedDetectionEvent;
        }

        return new Event
        (
            Type: DetectionType,
            Data: data?.AsReadOnly()
        );
    }

    public static Detection FromEvent(Event evt)
    {
        string? name = null;
        if (evt.TryGetDataValue("name", out var nameRaw))
        {
            name = nameRaw as string;
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

        string? speaker = null;
        if (evt.TryGetDataValue("speaker", out var speakerRaw))
        {
            speaker = speakerRaw as string;
        }

        Dictionary<string, object?>? context = null;
        if (evt.TryGetDataValue("context", out var ctxRaw))
        {
            context = ctxRaw as Dictionary<string, object?>;
        }

        return new Detection
        {
            Name = name,
            Timestamp = timestamp,
            Speaker = speaker,
            Context = context
        };
    }
}

