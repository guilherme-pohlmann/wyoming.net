using System.Runtime.InteropServices;

namespace Wyoming.Net.Core.Audio;

public static class AudioOp
{
    /// <summary>
    /// Convert float sampels to int16 sampels
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="dst">Must be samples.Length * 2</param>
    /// <returns></returns>
    public static int FloatToPcm16(ReadOnlySpan<float> samples, Span<byte> dst)
    {
        if (dst.Length < samples.Length * 2)
        {
            throw new ArgumentException("Destination span too small", nameof(dst));
        }

        int j = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            float s = samples[i];

            // Clamp just in case
            if (s > 1f)
            {
                s = 1f;
            }

            if (s < -1f)
            {
                s = -1f;
            }

            // Scale to 16-bit signed
            short val = (short)(s * 32767f);

            // Little-endian PCM16
            dst[j++] = (byte)(val & 0xFF);
            dst[j++] = (byte)((val >> 8) & 0xFF);
        }

        return j;
    }

    public static byte[] FloatToPcm16(ReadOnlySpan<byte> samples)
    {
        byte[] dst = new byte[samples.Length / 2];
        int written = FloatToPcm16(MemoryMarshal.Cast<byte, float>(samples), dst);

        Asserts.IsTrue(written == dst.Length, nameof(written));

        return dst;
    }

    public static int Pcm16ToFloat(ReadOnlySpan<byte> src, Span<float> dst)
    {
        if (src.Length % 2 != 0)
        {
            throw new ArgumentException("Source length must be even (PCM16)", nameof(src));
        }

        int sampleCount = src.Length / 2;

        if (dst.Length < sampleCount)
        {
            throw new ArgumentException("Destination span too small", nameof(dst));
        }

        int j = 0;

        for (int i = 0; i < sampleCount; i++)
        {
            // Little-endian PCM16
            short val = (short)(src[j] | (src[j + 1] << 8));
            j += 2;

            // Scale to float [-1.0, 1.0)
            // Using 32768f correctly maps -32768 -> -1.0
            dst[i] = val / 32768f;
        }

        return sampleCount;
    }
}