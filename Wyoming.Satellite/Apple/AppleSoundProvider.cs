#if IOS || MACCATALYST

using System.Runtime.InteropServices;
using AVFoundation;
using Microsoft.Extensions.Logging;
using Wyoming.Net.Core.Audio;

namespace Wyoming.Net.Satellite.Apple;

internal sealed partial class AppleSoundProvider : IMicInputProvider, ISpeakerProvider
{
    private AVAudioEngine? engine;
    private AVAudioInputNode? inputNode;
    private AVAudioConverter? micConverter;
    private AVAudioConverter? speakerConverter;
    private AVAudioPlayerNode? player;
    private CancellationTokenSource eventSignalingTokenSource = new();
    
    private readonly ILogger logger;
    private readonly AVAudioFormat micOutputFormat;
    private readonly SpscByteRingBuffer micRingBuffer;

    public AppleSoundProvider(ILogger logger)
    {
        this.logger = logger;
        IMicInputProvider mic = this;
        micOutputFormat = new AVAudioFormat(AVAudioCommonFormat.PCMFloat32, mic.Rate, (uint)mic.Channels, true);
        micRingBuffer = new SpscByteRingBuffer(capacity: 1 << 18);
    }

    private static AVAudioCommonFormat WidthToCommonFormat(int width)
    {
        return width switch
        {
            2 => AVAudioCommonFormat.PCMInt16,
            4 => AVAudioCommonFormat.PCMFloat32,
            _ => throw new NotSupportedException($"Unsupported with {width}")
        };
    }

    private static IntPtr ReadChannelPointer(AVAudioPcmBuffer buffer, int offset = 0)
    {
        // Channel data is a pointers array (float**)
        // Each pointer is a pointer to the channel data, like
        // [ ptr_to_channel_0 ][ ptr_to_channel_1 ]
        // Since we only handle 1 channel, we hardcoded get the first pointer which should point to channel 0
        
        IntPtr dataPtr = buffer.Format.CommonFormat switch
        {
            AVAudioCommonFormat.PCMInt16 => buffer.Int16ChannelData,
            AVAudioCommonFormat.PCMFloat32 => buffer.FloatChannelData,
            AVAudioCommonFormat.PCMInt32 => buffer.Int32ChannelData,
            _ => IntPtr.Zero
        };

        if (dataPtr == IntPtr.Zero)
        {
            throw new NotSupportedException($"Unsupported with {buffer.Format.CommonFormat}");
        }
        
        return Marshal.ReadIntPtr(dataPtr, 0);
    }
    
    public void Dispose()
    {
        Reset();
    }

    private void Reset()
    {
        eventSignalingTokenSource.Cancel();
        
        engine?.Stop();
        
        inputNode?.RemoveTapOnBus(0);
        inputNode = null;
        
        micConverter?.Dispose();
        micConverter = null;
        
        speakerConverter?.Dispose();
        speakerConverter = null;
        
        player?.Dispose();
        player = null;
        
        micDataAvailableEvent.Reset();
        speakerQueueEmptyEvent.Reset();
        
        engine?.Dispose();
        engine = null;
        
        micRingBuffer.Reset();

        if (!eventSignalingTokenSource.TryReset())
        {
            eventSignalingTokenSource.Dispose();
            eventSignalingTokenSource = new CancellationTokenSource();
        }
    }
}

#endif
