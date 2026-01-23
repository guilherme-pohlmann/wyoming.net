#if IOS || MACCATALYST

using System.Runtime.InteropServices;
using AVFoundation;
using Microsoft.Extensions.Logging;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Satellite.Apple;

internal sealed partial class AppleSoundProvider
{
    private int? speakerRate;
    private int? speakerChannel;
    private int? speakerWidth;
    private int speakerQueueSize = 0;
    private readonly ManualResetEventSlim speakerQueueEmptyEvent = new(false);
    
    int? ISpeakerProvider.Rate => speakerRate;
    
    int? ISpeakerProvider.Width => speakerWidth;

    int? ISpeakerProvider.Channels => speakerChannel;
    
    ValueTask ISpeakerProvider.StartAsync(int rate, int width, int channels)
    {
        Asserts.IsNotNull(engine);
        Asserts.IsNotNull(player);

        speakerRate = rate;
        speakerChannel = channels;
        speakerWidth = width;
        
        if (!player!.Playing)
        {
            player!.Play();
        }

        speakerQueueEmptyEvent.Reset();

        return ValueTask.CompletedTask;
    }

    ValueTask ISpeakerProvider.StopAsync()
    {
        if (Interlocked.CompareExchange(ref speakerQueueSize, 0, 0) > 0)
        {
            _ = Task.Run(speakerQueueEmptyEvent.Wait, eventSignalingTokenSource.Token).ContinueWith(parent =>
            {
                if (parent.IsCanceled || parent.IsFaulted)
                {
                    logger.LogWarning("ISpeakerProvider.StopAsync the parent task was not completed successfully. {status}", parent.Status);
                }
                
                player?.Stop();
                speakerConverter?.Dispose();
                speakerConverter = null;
            });
        }

        return ValueTask.CompletedTask;
    }
    
    private AVAudioPcmBuffer? SpeakerResample(byte[] samples)
    {
        Asserts.IsNotNull(speakerWidth, "Expected speaker width to be specified");
        Asserts.IsTrue(speakerWidth!.Value > 0, "Speaker width must be greater than zero at this point");
        Asserts.IsNotNull(speakerRate, "Expected speaker rate to be specified");
        Asserts.IsNotNull(speakerChannel, "Expected speaker channel to be specified");

        var bytesPerFrame = speakerWidth.Value;
        var inFrames = samples.Length / bytesPerFrame;
        var inSampleRate = speakerRate!.Value;
        using var inFormat = new AVAudioFormat(WidthToCommonFormat(speakerWidth.Value), inSampleRate, (uint)speakerChannel!.Value, false);
        var incomingBuffer = new AVAudioPcmBuffer(inFormat, (uint)inFrames);
        incomingBuffer.FrameLength = (uint)inFrames;

        FillAvAudioPcmBuffer(incomingBuffer, samples);

        var speakerOutputFormat = engine!.OutputNode.GetBusOutputFormat(0);
        var outFrames = (uint)Math.Ceiling(
            incomingBuffer.FrameLength * (speakerOutputFormat.SampleRate / inFormat.SampleRate));

        if (speakerConverter is null)
        {
            speakerConverter = new AVAudioConverter(incomingBuffer.Format, speakerOutputFormat);
        }

        var outBuffer = new AVAudioPcmBuffer(speakerOutputFormat, outFrames);
        var provided = false;
        
        var status = speakerConverter!.ConvertToBuffer(
            outBuffer,
            out var error,
            (uint _, out AVAudioConverterInputStatus outStatus) =>
            {
                if (!provided)
                {
                    provided = true;
                    outStatus = AVAudioConverterInputStatus.HaveData;
                    
                    // ReSharper disable once AccessToDisposedClosure
                    return incomingBuffer;
                }

                outStatus = AVAudioConverterInputStatus.NoDataNow;
                return null!;
            });
        
        incomingBuffer.Dispose();
        
        if (status == AVAudioConverterOutputStatus.Error)
        {
            logger.LogError("AVAudioConverter failed to convert: {error}", error?.LocalizedDescription ?? "Unknown error");
            outBuffer.Dispose();
            return null;
        }
        
        return outBuffer;
    }
    
    Task ISpeakerProvider.PlayAsync(byte[] samples, long? timestamp)
    {
        Asserts.IsNotNull(player);
        
        var buffer = SpeakerResample(samples);

        if (buffer is null)
        {
            logger.LogError("Failed to resample speaker samples");
            return Task.CompletedTask;
        }
        
        Interlocked.Increment(ref speakerQueueSize);
        
        player!.ScheduleBuffer(buffer, null, 0, AVAudioPlayerNodeCompletionCallbackType.PlayedBack,
            (_) =>
            {
                buffer.Dispose();
                var queueSize = Interlocked.Decrement(ref speakerQueueSize);

                if (queueSize == 0)
                {
                    speakerQueueEmptyEvent.Set();
                }
            });

        return Task.CompletedTask;
    }
    
    private static void FillAvAudioPcmBuffer(AVAudioPcmBuffer buffer, byte[] samples)
    {
        var channel0 = ReadChannelPointer(buffer);
        Marshal.Copy(samples, 0, channel0, samples.Length);

        if (buffer.Format.ChannelCount > 1)
        {
            var channel1 = ReadChannelPointer(buffer, 1);
            Marshal.Copy(samples, 0, channel1, samples.Length);
        }
    }
}

#endif