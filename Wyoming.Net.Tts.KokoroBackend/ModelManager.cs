namespace Wyoming.Net.Tts.KokoroBackend;

public static class ModelManager
{
    private const string KokoroFloat32 = "kokoro.onnx";
    private const string KokoroFloat16 = "kokoro-quant.onnx";
    private const string KokoroInt8 = "kokoro-quant-convinteger.onnx";
    private const string KokoroGpu = "kokoro-quant-gpu.onnx";

    public static readonly string[] Models = [KokoroFloat32, KokoroFloat16, KokoroInt8, KokoroGpu];
    
    private static string GetDownloadUrl(string model) => $"https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/{model}";
    
    private static bool IsDownloaded(string model)
    {
        return File.Exists(CrossPlatformHelper.GetModelPath(model));
    }
    
    public static async Task<string> LoadModelAsync(string model)
    {
        var modelPath = CrossPlatformHelper.GetModelPath(model);
        
        if (IsDownloaded(model))
        {
            return modelPath;
        }

        var downloadDir = Path.GetDirectoryName(modelPath);
        
        if (!Directory.Exists(downloadDir))
        {
            Directory.CreateDirectory(downloadDir!);
        }

        Console.WriteLine("Downloading model...");
        // Otherwise, download it to disk.
        using var client = new HttpClient();
        using var response = await client.GetAsync(GetDownloadUrl(model), HttpCompletionOption.ResponseHeadersRead);
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(modelPath, FileMode.CreateNew, FileAccess.Write);
        await responseStream.CopyToAsync(fileStream);

        Console.WriteLine("Model downloaded.");
        return modelPath;
    }
}