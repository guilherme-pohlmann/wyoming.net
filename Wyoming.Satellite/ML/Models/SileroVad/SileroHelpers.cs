namespace Wyoming.Net.Satellite.ML.Models.SileroVad;

public sealed class SileroVad
{
    private readonly float _threshold;
    private readonly float _negThreshold;
    private readonly int _windowSizeSample;
    private readonly float _minSpeechSamples;
    private readonly float _minSilenceSamples;
    private int _audioLengthSamples = 1760;
    private const float THRESHOLD_GAP = 0.15f; 
    
    private bool _triggered;
    private int _speechStart;
    private int _tempEnd;
    private int _currentSampleOffset;
    private bool _speechDetected;
    
    public SileroVad(float threshold, int samplingRate, int minSpeechDurationMs, int minSilenceDurationMs)
    {
        _threshold = threshold;
        _negThreshold = threshold - THRESHOLD_GAP;
        _windowSizeSample = 512;
        _minSpeechSamples = samplingRate * minSpeechDurationMs / 1000f;
        _minSilenceSamples = samplingRate * minSilenceDurationMs / 1000f;
    }
    
    public void Reset()
    {
        _triggered = false;
        _speechStart = 0;
        _tempEnd = 0;
        _currentSampleOffset = 0;
        _speechDetected = false;
    }
    
    public enum VadEvent
    {
        None,
        SpeechStart,
        SpeechEnd
    }

    private VadEvent ProcessProbability2(float speechProb)
    {
        int currentSample = _currentSampleOffset;

        if (speechProb >= _threshold && !_triggered)
        {
            _triggered = true;
            _speechStart = currentSample;
            _tempEnd = 0;
            return VadEvent.SpeechStart;
        }

        if (!_triggered)
            return VadEvent.None;

        if (speechProb >= _threshold)
        {
            _tempEnd = 0;
            return VadEvent.None;
        }

        if (speechProb < _negThreshold)
        {
            if (_tempEnd == 0)
                _tempEnd = currentSample;

            if (currentSample - _tempEnd < _minSilenceSamples)
                return VadEvent.None;

            // Silence long enough — speech segment ended
            bool wasValidSpeech = (_tempEnd - _speechStart) > _minSpeechSamples;
            _triggered = false;
            _tempEnd = 0;

            return wasValidSpeech ? VadEvent.SpeechEnd : VadEvent.None;
        }

        return VadEvent.None;
    }
    
    public List<VadEvent> HasDetectedSpeech(ReadOnlySpan<float> predictions)
    {
        //var result = new List<VadEvent>();
        int offset = 0;
        int pred = 0;
        while (offset + _windowSizeSample <= 1760)
        {
           ProcessProbability2(predictions[pred]);
            pred++;

            offset += _windowSizeSample;
            _currentSampleOffset += _windowSizeSample;
        }

        return null;
    }

    private void ProcessProbability(float speechProb)
    {
        int currentSample = _currentSampleOffset;

        if (speechProb >= _threshold && !_triggered)
        {
            _triggered = true;
            _speechStart = currentSample;
            _tempEnd = 0;
            return;
        }

        if (!_triggered)
            return;

        // Speech resumed — reset silence tracking
        if (speechProb >= _threshold)
        {
            _tempEnd = 0;

            // Check if we've been in speech long enough
            if (currentSample - _speechStart > _minSpeechSamples)
                _speechDetected = true;

            return;
        }

        // Below negative threshold — track silence
        if (speechProb < _negThreshold)
        {
            if (_tempEnd == 0)
                _tempEnd = currentSample;

            // Silence hasn't lasted long enough
            if (currentSample - _tempEnd < _minSilenceSamples)
            {
                // Still check ongoing speech length
                if (_tempEnd - _speechStart > _minSpeechSamples)
                    _speechDetected = true;

                return;
            }

            // Silence confirmed — evaluate the speech region
            if (_tempEnd - _speechStart > _minSpeechSamples)
                _speechDetected = true;

            // Reset for next potential speech region
            _triggered = false;
            _tempEnd = 0;
        }
    }
}