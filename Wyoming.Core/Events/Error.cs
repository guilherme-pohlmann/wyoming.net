namespace Wyoming.Net.Core.Events;

public sealed class Error : IEventable
{
    private const string ErrorType = "error";

    public string Text { get; init; }

    public string? Code { get; init; }

    public Error(string text)
    {
        Text = text;
    }

    public static bool IsType(string eventType) =>
        ErrorType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>
        {
            { "text", Text }
        };

        if (!string.IsNullOrEmpty(Code))
        {
            data["code"] = Code;
        }

        return new Event(
            Type: ErrorType,
            Data: data.AsReadOnly()
        );
    }

    public static Error FromEvent(Event evt)
    {
        string text = evt.GetDataValueOrDefault<string>("text") ?? string.Empty;
        string? code = evt.GetDataValueOrDefault<string>("code");

        return new Error(text)
        {
            Code = code
        };
    }
}

