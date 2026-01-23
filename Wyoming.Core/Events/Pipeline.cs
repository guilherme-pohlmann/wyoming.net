namespace Wyoming.Net.Core.Events;

public static class PipelineStage
{
    /// <summary>
    /// Wake word detection
    /// </summary>
    public const string Wake = "wake";

    /// <summary>
    /// Speech-to-text (a.k.a automated speech recogition)
    /// </summary>
    public const string ASR = "asr";

    /// <summary>
    /// Intent recognition
    /// </summary>
    public const string Intent = "intent";

    /// <summary>
    /// Intent handling
    /// </summary>
    public const string Handle = "handle";

    /// <summary>
    /// Text-to-speech
    /// </summary>
    public const string TTS = "tts";
}

public sealed class RunPipeline : IEventable
{
    private const string RunPipelineType = "run-pipeline";

    public RunPipeline(string startStage, string endStage)
    {
        static bool IsAny(string target, params ReadOnlySpan<string> args)
        {
            foreach (var arg in args)
            {
                if (target.Equals(arg))
                {
                    return true;
                }
            }

            return false;
        }

        var startValid = true;
        var endValid = true;

        if (startStage.Equals(PipelineStage.Wake, StringComparison.OrdinalIgnoreCase))
        {
            endValid = IsAny(endStage, PipelineStage.Wake, PipelineStage.Handle, PipelineStage.Intent, PipelineStage.TTS, PipelineStage.ASR);
        }
        else if (startStage.Equals(PipelineStage.ASR, StringComparison.OrdinalIgnoreCase))
        {
            endValid = IsAny(endStage, PipelineStage.Handle, PipelineStage.Intent, PipelineStage.TTS, PipelineStage.ASR);
        }
        else if (startStage.Equals(PipelineStage.Intent, StringComparison.OrdinalIgnoreCase))
        {
            endValid = IsAny(endStage, PipelineStage.Handle, PipelineStage.Intent, PipelineStage.TTS);
        }
        else if (startStage.Equals(PipelineStage.Handle, StringComparison.OrdinalIgnoreCase))
        {
            endValid = IsAny(endStage, PipelineStage.Handle, PipelineStage.TTS);
        }
        else if (startStage.Equals(PipelineStage.TTS, StringComparison.OrdinalIgnoreCase))
        {
            endValid = IsAny(endStage, PipelineStage.TTS);
        }
        else
        {
            startValid = false;
        }

        if (!startValid)
        {
            throw new ArgumentException($"Invalid startStage: {startStage}");
        }

        if (!endValid)
        {
            throw new ArgumentException($"Invalid endStage: {startStage}");
        }

        StartStage = startStage;
        EndStage = endStage;
    }

    public string StartStage { get; }

    public string EndStage { get; }

    public string? WakeWordName { get; init; }

    public bool RestartOnEnd { get; init; }

    public List<string>? WakeWordNames { get; init; }

    public string? AnnounceText { get; init; }

    public static bool IsType(string eventType) => RunPipelineType.Equals(eventType, StringComparison.OrdinalIgnoreCase);

    public Event ToEvent()
    {
        var data = new Dictionary<string, object>()
        {
            { "start_stage", StartStage },
            { "end_stage", EndStage },
            { "restart_on_end", RestartOnEnd }
        };

        if (!string.IsNullOrEmpty(WakeWordName))
        {
            data["wake_word_name"] = WakeWordName;
        }

        if (WakeWordNames?.Count > 0)
        {
            data["wake_word_names"] = WakeWordNames;
        }

        if (!string.IsNullOrEmpty(AnnounceText))
        {
            data["announce_text"] = AnnounceText;
        }

        return new Event(RunPipelineType, data.AsReadOnly());
    }

    public static RunPipeline FromEvent(Event evt)
    {
        return new RunPipeline(evt.GetDataValueOrDefault<string>("start_stage")!, evt.GetDataValueOrDefault<string>("end_stage")!)
        {
            AnnounceText = evt.GetDataValueOrDefault<string>("announce_text"),
            RestartOnEnd = evt.GetDataValueOrDefault<bool>("restart_on_end"),
            WakeWordName = evt.GetDataValueOrDefault<string>("wake_word_name"),
            WakeWordNames = evt.GetDataValueOrDefault<IEnumerable<string>>("wake_word_names")?.ToList()
        };
    }
}
