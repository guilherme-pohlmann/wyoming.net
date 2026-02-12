using System.Text;

namespace Wyoming.Net.Satellite;

public readonly struct WavInfo
{
    public int SampleRate { get; init; }

    public int Channels { get; init; }

    public int BitsPerSample { get; init; }

    public int BytesPerSample => BitsPerSample / 8;
}

public static class WavHelper
{
    public static WavInfo ReadWavInfo(byte[] wav)
    {
        using var stream = new MemoryStream(wav);
        using var br = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        // RIFF
        if (new string(br.ReadChars(4)) != "RIFF")
        {
            throw new InvalidDataException("Not a WAV file");
        }

        br.ReadInt32(); // file size

        if (new string(br.ReadChars(4)) != "WAVE")
        {
            throw new InvalidDataException("Not a WAV file");
        }

        while (stream.Position < stream.Length)
        {
            string chunkId = new string(br.ReadChars(4));
            int chunkSize = br.ReadInt32();

            if (chunkId == "fmt ")
            {
                short audioFormat = br.ReadInt16(); // 1 = PCM
                short channels = br.ReadInt16();
                int sampleRate = br.ReadInt32();

                br.ReadInt32(); // byteRate
                br.ReadInt16(); // blockAlign
                short bitsPerSample = br.ReadInt16();

                if (audioFormat != 1 && audioFormat != 3)
                {
                    throw new NotSupportedException("Only PCM or IEEE float supported");
                }

                return new WavInfo
                {
                    SampleRate = sampleRate,
                    Channels = channels,
                    BitsPerSample = bitsPerSample
                };
            }

            // skip unknown chunk
            stream.Position += chunkSize;
        }

        throw new InvalidDataException("fmt chunk not found");
    }

    public static byte[] ReadWavData(byte[] wav)
    {
        using var stream = new MemoryStream(wav);
        using var br = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        // 1. Validate Header
        if (new string(br.ReadChars(4)) != "RIFF")
            throw new InvalidDataException("Not a RIFF file");

        br.ReadInt32(); // Skip file size

        if (new string(br.ReadChars(4)) != "WAVE")
            throw new InvalidDataException("Not a WAVE file");

        // 2. Search for the "data" chunk
        while (stream.Position < stream.Length)
        {
            // Ensure we have enough bytes left to read a chunk header (8 bytes)
            if (stream.Position + 8 > stream.Length) break;

            string chunkId = new string(br.ReadChars(4));
            int chunkSize = br.ReadInt32();

            if (chunkId == "data")
            {
                // Found it! Read and return the raw samples
                return br.ReadBytes(chunkSize);
            }

            // Move to the next chunk
            // Note: Chunk sizes are usually word-aligned, but in practice, 
            // most WAV writers handle this by including the padding in chunkSize
            stream.Position += chunkSize;
        }

        throw new InvalidDataException("Data chunk not found");
    }
}