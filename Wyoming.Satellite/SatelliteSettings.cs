namespace Wyoming.Net.Satellite;


public sealed record MicSettings 
{
    public double VolumeMultiplier { get; init; } = 1.0;

    public int AutoGain { get; init; } = 0;

    public int NoiseSuppression { get; init; } = 0;

    public int Rate { get; init; } = 16000;

    public int Width { get; init; } = 2;

    public int Channels { get; init; } = 1;

    public int SamplesPerChunk { get; init; } = 1024;

    public bool MuteDuringAwakeWav { get; init; } = true;

    public double SecondsToMuteAfterAwakeWav { get; init; } = 0.5;

    public int? ChannelIndex { get; init; }

    public bool NeedsWebRtc => AutoGain > 0 || NoiseSuppression > 0;

    public bool NeedsProcessing => VolumeMultiplier != 1.0 || NeedsWebRtc;
}

public sealed record SndSettings
{
    public double VolumeMultiplier { get; init; } = 1.0;

    public string? AwakeWav { get; init; }

    public string? DoneWav { get; init; }

    public int Rate { get; init; } = 22050;

    public int Width { get; init; } = 2;

    public int Channels { get; init; } = 1;

    public int SamplesPerChunk { get; init; } = 1024;

    public bool DisconnectAfterStop { get; init; } = true;

    public bool NeedsProcessing => Enabled && VolumeMultiplier != 1.0;

    public bool Enabled { get; init; } = true;
}

public sealed record WakeWordAndPipeline
{
    public string Name { get; init; } = string.Empty;

    public string? Pipeline { get; init; }
}

public sealed record WakeSettings
{
    public string? Name { get; init; }

    public int Rate { get; init; } = 16000;

    public int Width { get; init; } = 2;

    public int Channels { get; init; } = 1;

    public int? RefractorySeconds { get; init; } = 5;

    public int MaxPatience { get; init; } = 15;

    public float PredictionThreshold { get; init; } = 0.5f;

    public bool Enabled { get; init; }
}

public sealed record VadSettings
{
    public bool Enabled { get; init; } = false;

    public double Threshold { get; init; } = 0.5;

    public int TriggerLevel { get; init; } = 1;

    public double BufferSeconds { get; init; } = 2.0;

    public double? WakeWordTimeout { get; init; } = 5.0;
}

public sealed record TimerSettings
{
    public IReadOnlyList<string>? Started { get; init; }

    public IReadOnlyList<string>? Updated { get; init; }

    public IReadOnlyList<string>? Cancelled { get; init; }

    public IReadOnlyList<string>? Finished { get; init; }

    public string? FinishedWav { get; init; }

    public int FinishedWavPlays { get; init; } = 1;

    public double FinishedWavDelay { get; init; } = 0;
}

public sealed record SatelliteSettings
{
    public MicSettings Mic { get; init; } = new();

    public VadSettings Vad { get; init; } = new();

    public WakeSettings Wake { get; init; } = new();

    public SndSettings Snd { get; init; } = new();

    //public TimerSettings Timer { get; init; } = new();
}
