using Microsoft.Extensions.Logging;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;
using Wyoming.Net.Tts;
using Wyoming.Net.Tts.Backend.Kokoro;

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


var server = new AsyncTcpServer(
    "0.0.0.0",
    10201,
    (client, server, loggerFactory) => new SynthesizeEventHandler(client, server, loggerFactory, info),
    factory);
    
await server.RunAsync();