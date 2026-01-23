namespace Wyoming.Net.Core.Events;

public sealed class RunSatellite : IEventable
{
    private const string RunSatelliteType = "run-satellite";

    public static bool IsType(string eventType) => RunSatelliteType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        return new Event(RunSatelliteType);
    }

    public static RunSatellite FromEvent(Event evt)
    {
        return new RunSatellite();
    }
}

public sealed class PauseSatellite : IEventable
{
    private const string PauseSatelliteType = "pause-satellite";

    public static bool IsType(string eventType) =>
        PauseSatelliteType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        return new Event
        (
            Type: PauseSatelliteType
        );
    }

    public static PauseSatellite FromEvent(Event evt)
    {
        return new PauseSatellite();
    }
}
