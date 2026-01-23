#if IOS || MACCATALYST

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AVFoundation;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using Wyoming.Net.Core;
using Wyoming.Net.Core.Audio;
using Exception = System.Exception;
using Math = System.Math;

namespace Wyoming.Net.Satellite.Apple;

internal sealed partial class AppleSoundProvider
{
    private readonly ManualResetEventSlim micDataAvailableEvent = new(false);
    private int readInProgress = 0; 
    
    int IMicInputProvider.Rate => 16000;
    
    int IMicInputProvider.Channels => 1;

    int IMicInputProvider.Width => sizeof(float);

    private IMicInputProvider MicInputProvider => this;
    
    ValueTask IMicInputProvider.StartRecordingAsync()
    {
        if (inputNode is not null)
        {
            return ValueTask.CompletedTask;
        }
        
        SessionManager.StartSession(logger, MicInputProvider.Rate);

        engine = new AVAudioEngine();
        inputNode = engine.InputNode;
        var outputNode = engine.OutputNode;
        
        player = new AVAudioPlayerNode();
        engine!.AttachNode(player);

        var inputNodeFormat = inputNode.GetBusInputFormat(0);
        var outputNodeFormat = outputNode.GetBusOutputFormat(0);

        logger.LogInformation("iOS INPUT Node format: Rate: {rate}, ChannelCount: {ChannelCount}, Format: {format}",
            inputNodeFormat.SampleRate, 
            inputNodeFormat.ChannelCount, 
            inputNodeFormat.CommonFormat.ToString());
        
        logger.LogInformation("iOS OUTPUT Node format: Rate: {rate}, ChannelCount: {ChannelCount}, Format: {format}",
            outputNodeFormat.SampleRate, 
            outputNodeFormat.ChannelCount, 
            outputNodeFormat.CommonFormat.ToString());

        if (inputNodeFormat.ChannelCount > MicInputProvider.Channels)
        {
            Reset();
            throw new NotSupportedException("Only single channel audio inputs are supported");
        }

        if (inputNodeFormat.Interleaved)
        {
            Reset();
            throw new NotSupportedException("Interleaved inputs are not supported");
        }

        if ((int)inputNodeFormat.SampleRate != (int)micOutputFormat.SampleRate)
        {
            micConverter = new AVAudioConverter(inputNodeFormat, micOutputFormat);
        }
        
        try
        {
            engine.Connect(player, engine.MainMixerNode, outputNodeFormat);
            
            engine.Prepare();
            
            inputNode.InstallTapOnBus(
                bus: 0,
                bufferSize: 8192,
                format: inputNodeFormat,
                tapBlock: OnMicAudio
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to install tap on bus");
            throw;
        }

        if (!engine.StartAndReturnError(out var error))
        {
            logger.LogError("Failed to start engine: {error}", error.LocalizedDescription);
            throw new RuntimeException(error.LocalizedDescription);
        }

        return ValueTask.CompletedTask;
    }

    ValueTask IMicInputProvider.StopRecordingAsync()
    {
        Reset();
        
        SessionManager.EndSession(logger);

        return ValueTask.CompletedTask;
    }

    Task<long?> IMicInputProvider.ReadAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            // The upstream app is designed to call this on a single thread, but leaving a sanity check here anyways
            // Shouldn't be too hard to support thread safety but keeping things simple for now
            Asserts.IsFalse(Interlocked.Exchange(ref readInProgress, 1) == 1,
                "ReadAsync was called by multiple threads");
            
            while (micRingBuffer.AvailableToRead < buffer.Length)
            {
                // TODO: maybe link with eventSignalingTokenSource ?
                micDataAvailableEvent.Wait(cancellationToken);
            }

            Asserts.IsTrue(micRingBuffer.Read(buffer), "Expected to be able to read at this point");

            if (micRingBuffer.AvailableToRead == 0)
            {
                micDataAvailableEvent.Reset();
            }

            return Task.FromResult((long?)0L);
        }
        finally
        {
            Volatile.Write(ref readInProgress, 0);
        }
    }
    
    private ReadOnlySpan<byte> NativeAudioBufferToSpan(AVAudioPcmBuffer buffer)
    {
        Asserts.IsTrue(buffer.Format.CommonFormat == AVAudioCommonFormat.PCMFloat32, "Expected PCMFloat32 format");
        Asserts.IsTrue(buffer.FloatChannelData != IntPtr.Zero, "Expected FloatChannelData");

        IntPtr channel0 = ReadChannelPointer(buffer);
        
        // Harmless hack to get a Span from an IntPtr without unsafe blocks
        // Got this from: https://stackoverflow.com/questions/73917378/how-to-copy-spanbyte-contents-to-location-pointed-by-intptr
        // Posted by Marcin Rybak - Thanks!
        var bytes = MemoryMarshal.CreateSpan(
            ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), channel0), 
            (int)buffer.FrameLength * MicInputProvider.Width
            );
        
        return bytes;
    }
    
    private static uint EstimateResamplingSize(
        int frameLength,
        int width,
        int nChannels,
        int inRate,
        int outrate)
    {
        int bytesPerFrame = width * nChannels;
        int inputFrames = frameLength / bytesPerFrame;
        int outputFrames = (int)Math.Ceiling(inputFrames * (outrate / (double)inRate));
        return (uint)(outputFrames * bytesPerFrame);
    }
    
    private void OnMicAudio(AVAudioPcmBuffer incomingBuffer, AVAudioTime when)
    {
        // In case we get any callbacks after a call to Stop/Dispose
        if (inputNode is null)
        {
            logger.LogInformation("Got an audio buffer when disposed");
            return;
        }
        
        var outgoingBuffer = incomingBuffer;

        // if converter is not null the bus input format is different from what we need
        if (micConverter is not null)
        {
            Asserts.IsTrue(incomingBuffer.Format == micConverter.InputFormat, "Expected bus input format to equal AVAudioConverter input format");
            outgoingBuffer = MicResample(incomingBuffer);
            
            if (outgoingBuffer is null)
            {
                return;
            }
        }
        
        // Bit dangerous because this Span must never outlive the buffer
        // But we are copying right after so we should be safe
        var bytes = NativeAudioBufferToSpan(outgoingBuffer);

        bool wasEmpty = micRingBuffer.AvailableToRead == 0;
        micRingBuffer.Write(bytes);

        if (wasEmpty)
        {
            micDataAvailableEvent.Set();
        }

        // I think the framework owns incoming buffer so no need to dispose
        // But if we resampled then we own outgoingBuffer
        if (!ReferenceEquals(incomingBuffer, outgoingBuffer))
        {
            outgoingBuffer.Dispose();
        }
    }

    private AVAudioPcmBuffer? MicResample(AVAudioPcmBuffer incomingBuffer)
    {
        uint frameLength = EstimateResamplingSize(
            (int)incomingBuffer.FrameLength,
            MicInputProvider.Width, 
            MicInputProvider.Channels, 
            (int)incomingBuffer.Format.SampleRate, 
            MicInputProvider.Rate);
        
        var outBuffer = new AVAudioPcmBuffer(micOutputFormat, frameLength);
        var provided = false;
        
        var status = micConverter!.ConvertToBuffer(
            outBuffer,
            out var error,
            (uint _, out AVAudioConverterInputStatus outStatus) =>
            {
                if (provided)
                {
                    outStatus = AVAudioConverterInputStatus.NoDataNow;
                    return null!;
                }

                provided = true;
                outStatus = AVAudioConverterInputStatus.HaveData;
                return incomingBuffer;
            });
        
        if (status == AVAudioConverterOutputStatus.Error)
        {
            logger.LogError("AVAudioConverter failed to convert: {error}", error?.LocalizedDescription ?? "Unknown error");
            outBuffer.Dispose();
            return null;
        }
            
        return outBuffer;
    }
}

#endif