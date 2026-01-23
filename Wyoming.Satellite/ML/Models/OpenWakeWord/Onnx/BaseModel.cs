using Microsoft.ML.OnnxRuntime;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

public abstract class BaseModel : IDisposable
{
    internal static readonly RunOptions DefaultRunOptions = new();
    internal static readonly SessionOptions DefaultSessionOptions = new();

    static BaseModel()
    {
#if ANDROID
        DefaultSessionOptions.AppendExecutionProvider_Nnapi(NnapiFlags.NNAPI_FLAG_USE_NONE);
#endif
#if IOS || MACCATALYST
       DefaultSessionOptions.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_USE_CPU_AND_GPU); 
#endif
    }

    protected readonly InferenceSession session;

    protected BaseModel(byte[] model)
    {
        session  = new InferenceSession(model, DefaultSessionOptions);
    }

    ~BaseModel()
    {
        Dispose();
    }

    public void Dispose()
    {
        session.Dispose();
        GC.SuppressFinalize(this);
    }
}
