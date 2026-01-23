namespace Wyoming.Net.Tts.Backend.Kokoro;

public static class PauseAfterSegmentStrategy
{
    private const float CommaPause = 0.1f;

    private const float PeriodPause = 0.5f;

    private const float QuestionMarkPause = 0.5f;

    private const float ExclamationMarkPause = 0.5f;

    private const float NewLinePause = 0.5f;

    private const float OthersPause = 0.5f;

    public static float GetPauseForSegment(char c)
    {
        return c switch
        {
            ',' => CommaPause,
            '.' => PeriodPause,
            '?' => QuestionMarkPause,
            '!' => ExclamationMarkPause,
            '\n' => NewLinePause,
            '¿' => OthersPause,
            _ => 0
        };
    }
}
