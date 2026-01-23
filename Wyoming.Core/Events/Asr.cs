
namespace Wyoming.Net.Core.Events;

public sealed class Transcript : IEventable
{
    private const string TranscriptType = "transcript";

    public string Text { get; init; }

    public Dictionary<string, object>? Context { get; init; }

    public string? Language { get; init; }

    public Transcript(string text)
    {
        Text = text;
    }

    public static bool IsType(string eventType) =>
        TranscriptType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>
        {
            { "text", Text }
        };

        if (!string.IsNullOrEmpty(Language))
        {
            data["language"] = Language;
        }

        if (Context != null)
        {
            data["context"] = Context;
        }

        return new Event(Type: TranscriptType, Data: data.AsReadOnly());
    }

    public static Transcript FromEvent(Event evt)
    {
        string text = evt.Data?["text"] as string ?? string.Empty;

        string? language = null;

        if (evt.TryGetDataValue("language", out var langRaw))
        {
            language = langRaw as string;
        }

        Dictionary<string, object>? context = null;

        if (evt.TryGetDataValue("context", out var ctxRaw))
        {
            context = ctxRaw as Dictionary<string, object>;
        }

        return new Transcript(text)
        {
            Language = language,
            Context = context
        };
    }
}

