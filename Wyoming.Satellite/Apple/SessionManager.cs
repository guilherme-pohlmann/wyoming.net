#if IOS || MACCATALYST

using AVFoundation;
using Microsoft.Extensions.Logging;

namespace Wyoming.Net.Satellite.Apple;

internal sealed class SessionManager
{
    public static bool StartSession(ILogger logger, int rate)
    {
        var session = AVAudioSession.SharedInstance();

        var error = session.SetCategory(
            AVAudioSessionCategory.PlayAndRecord,
            AVAudioSessionCategoryOptions.AllowBluetooth
        );

        if (error is not null)
        {
            logger.LogError("Error setting session category: {error}", error.LocalizedDescription);
            return false;
        }
        
        if (!session.SetMode(AVAudioSessionMode.Default, out var modeError))
        {
            logger.LogWarning("Failed to set mode: {error}", modeError.LocalizedDescription);    
        }

        if (!session.SetPreferredSampleRate(rate, out var rateError))
        {
            logger.LogWarning("Failed to set preferred rate: {error}", rateError.LocalizedDescription);
        }
               
        if (!session.SetPreferredIOBufferDuration(0.08, out var durationError))
        {
            logger.LogWarning("Failed to set preferred duration: {error}", durationError.LocalizedDescription);
        }
        
        error = session.SetActive(true);

        if (error is not null)
        {
            logger.LogError("Failed to activate session: {error}", error.LocalizedDescription);
            return false;
        }
        
        var currentRoute = session.CurrentRoute;
        
        foreach (var output in currentRoute.Outputs)
        {
            logger.LogInformation("Audio is currently playing to: {port}", output.PortName);
        }
        return true;
    }

    public static void EndSession(ILogger logger)
    {
        var session = AVAudioSession.SharedInstance();
        
        var error = session.SetActive(false);

        if (error is not null)
        {
            logger.LogError("Failed to deactivate session: {error}", error.LocalizedDescription);
        }
    }
}

#endif