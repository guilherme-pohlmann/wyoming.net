using System.CommandLine;
using Microsoft.Extensions.Logging;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;
using Wyoming.Net.Tts;
using Wyoming.Net.Tts.KokoroBackend;

//TODO: I want to eventually load the backend dynamically and decouple this project from referencing backends directly

var hostOption = new Option<string>("--host")
{
    DefaultValueFactory = _ => "0.0.0.0",
};

var portOption = new Option<int>("--port")
{
    DefaultValueFactory = _ => 10201,
};

var modelOption = new Option<string>("--model")
{
    Description = $"Available models are: {string.Join(',', ModelManager.Models)}",
    Required = true
};
modelOption.Validators.Add(result =>
{
    var value = result.GetResult(modelOption)?.GetValueOrDefault<string>();

    if (!ModelManager.Models.Contains(value))
    {
        result.AddError("Invalid model");
    }
});

var useCudaOption = new Option<bool>("--useCuda")
{
    DefaultValueFactory = _ => false,
};
var defaultVoiceOption = new Option<string>("--defaultVoice")
{
    DefaultValueFactory = _ => "pf_dora",
};

var rootCommand = new RootCommand()
{
    hostOption,
    portOption,
    modelOption,
    useCudaOption,
    defaultVoiceOption
};

var argsResult = rootCommand.Parse(Environment.CommandLine);

if (argsResult.Errors.Count > 0)
{
    foreach (var error in argsResult.Errors)
    {
        Console.WriteLine(error);
    }

    return;
}

var info = new Info(null)
{
    Tts = [new TtsProgram()
    {
        Attribution = new Attribution()
        {
            Name =  "Wyoming.Net.Tts",
            Url = "https://github.com/guilherme-pohlmann/wyoming-net"
        },
        Description = "Wyoming .Net Local TTS satellite",
        Installed = true,
        Name = "Wyoming.Net.Tts",
        SupportsSynthesizeStreaming = true,
        Voices = ListVoices(),
        Version = "0.0.1"
    }]
};

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information); 
});

var model = argsResult.GetRequiredValue(modelOption);

UserSettings.Model = model;
UserSettings.UseCuda = argsResult.GetRequiredValue(useCudaOption);
UserSettings.DefaultVoice = argsResult.GetRequiredValue(defaultVoiceOption);

var server = new AsyncTcpServer(
    argsResult.GetRequiredValue(hostOption),
    argsResult.GetRequiredValue(portOption),
    (client, server, loggerFactory) => new SynthesizeEventHandler(client, server, loggerFactory, () => new KokoroBackend(useCuda: UserSettings.UseCuda), info),
    factory);
    
await server.RunAsync();
return;

static IEnumerable<TtsVoice> ListVoices()
{
    return KokoroVoice.EnumerateVoices(false).Select(kokoroVoice => new TtsVoice()
    {
        Name = kokoroVoice.Name,
        Attribution = new Attribution(),
        Description = kokoroVoice.Name,
        Installed = true,
        Languages = [kokoroVoice.GetLangCode()],
        Speakers =
        [
            new TtsVoiceSpeaker()
            {
                Name = $"{kokoroVoice.Gender.ToString()} - {kokoroVoice.Name}"
            }
        ],
        Version = null
    });
}