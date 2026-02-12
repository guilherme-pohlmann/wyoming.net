#if TIZEN8_0_OR_GREATER
using Tizen.MachineLearning.Inference;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Tizen;

public abstract class TizenModel : IDisposable
{
    protected readonly SingleShot engine;

    public TizenModel(string modelPath)
    {
        engine = new SingleShot(modelPath);
    }

    public void Dispose()
    {
        engine.Dispose();
        GC.SuppressFinalize(this);
    }
}

#endif