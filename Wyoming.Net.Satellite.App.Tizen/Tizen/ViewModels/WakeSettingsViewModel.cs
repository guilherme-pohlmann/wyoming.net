using System;
using System.Threading.Tasks;
using Wyoming.Net.Core;
using Wyoming.Net.Satellite.App.Tz.Platform;
using Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Tizen;

namespace Wyoming.Net.Satellite.App.Tz.ViewModels;

public sealed class WakeSettingsViewModel
{
    public string? Model { get;set; } = "alexa";

    public bool Enabled { get; set; } = true;

    public int Rate { get; set; } = 16000;

    public int Width { get; set; } = 2;

    public int Channels {get; set; } = 1;

    public int RefractorySeconds { get; set; } = 5;

    public int MaxPatience { get; set; } = 20;

    public float PredictionThreshold { get; set; } = 0.5f;

    public bool IsValid(out string? message)
    {
        message = null;

        if(Enabled && string.IsNullOrEmpty(Model))
        {
            message = "Please enter wake word model";
            return false;  
        }

        return true;
    }

    public async Task<OpenWakeWordModels> GetModelsAsync()
    {
        Asserts.IsTrue(Enabled);
        Asserts.IsNotNull(Model);

        var melspectrogramModel = new MelspectrogramModel(TizenAssetReader.GetResourcePath("melspectrogram.tflite"));
        var embeddingModel = new EmbeddingModel(TizenAssetReader.GetResourcePath("embedding_model.tflite"));
        var wakeWordModel = new WakeWordModel(TizenAssetReader.GetResourcePath(GetWakeModelFile(Model!)));

        return new OpenWakeWordModels(embeddingModel, melspectrogramModel, wakeWordModel);
    }

    public WakeSettings ToSatelliteSettings()
    {
        return new WakeSettings()
        {
            Channels = Channels,
            Enabled = Enabled,
            MaxPatience = MaxPatience,
            PredictionThreshold = PredictionThreshold,
            Name = Model,
            Rate = Rate,
            RefractorySeconds = RefractorySeconds,
            Width = Width
        };
    }

    private static string GetWakeModelFile(string model)
    {
        return model switch
        {
            "alexa" => "alexa_v0.1.tflite",
            _ => throw new NotImplementedException(),
        };
    }
}