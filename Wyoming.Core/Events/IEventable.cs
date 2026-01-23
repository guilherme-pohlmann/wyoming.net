namespace Wyoming.Net.Core.Events;

public interface IEventable
{
    Event ToEvent();
}
