namespace Wyoming.Net.Core.Events;

public sealed class Ping : IEventable
{
    private static readonly Ping CachedPing = new();
    private const string PingType = "ping";

    public string? Text { get; init; }

    public static bool IsType(string eventType) => PingType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public static Ping FromEvent(Event evt)
    {
        if (evt.TryGetDataValue("text", out var raw))
        {
            string? text = raw as string;

            return new Ping
            {
                Text = text
            };
        }

        return CachedPing;
    }

    public Event ToEvent()
    {
        return new Event
        (
            Type: PingType,
            Data: string.IsNullOrEmpty(Text) ? null : new Dictionary<string, object>(1)
            {
                { "text", Text }
            }.AsReadOnly()
        );
    }
}


public sealed class Pong : IEventable
{
    private const string PongType = "pong";
    private static readonly Pong CachedPong = new();
    private static readonly Event CachedPongEvent = new(PongType);

    public string? Text { get; init; }

    public static bool IsType(string eventType) => PongType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public static Pong FromEvent(Event evt)
    {
        string? text = null;

        if (evt.TryGetDataValue("text", out var raw))
        {
            text = raw as string;
        }

        return new Pong
        {
            Text = text
        };
    }

    public static Pong FromPing(Ping ping)
    {
        if(string.IsNullOrEmpty(ping.Text))
        {
            return CachedPong;
        }

        return new Pong
        {
            Text = ping.Text
        };
    }

    public Event ToEvent()
    {
        if(string.IsNullOrEmpty(Text))
        {
            return CachedPongEvent;
        }

        return new Event
        (
            Type: PongType,
            Data: string.IsNullOrEmpty(Text) ? null : new Dictionary<string, object>(1)
            {
                { "text", Text }
            }.AsReadOnly()!
        );
    }
}
