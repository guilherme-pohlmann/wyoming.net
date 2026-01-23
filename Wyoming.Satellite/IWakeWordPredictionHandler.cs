namespace Wyoming.Net.Satellite;

public interface IWakeWordPredictionHandler
{
    ValueTask OnPredictionAsync();
}
