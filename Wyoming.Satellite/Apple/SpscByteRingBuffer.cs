#if IOS || MACCATALYST

namespace Wyoming.Net.Satellite.Apple;

//TODO: maybe move this to core?

internal sealed class SpscByteRingBuffer
{
    private readonly byte[] buffer;
    private readonly int capacity;

    // These are byte indices, not frame indices
    private int writePos;
    private int readPos;

    public SpscByteRingBuffer(int capacity)
    {
        // capacity must be power of 2 for fast modulo
        if ((capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be power of 2");
        }

        this.capacity = capacity;
        buffer = new byte[capacity];
    }

    public int AvailableToRead
    {
        get
        {
            int w = Volatile.Read(ref writePos);
            int r = Volatile.Read(ref readPos);
            return (w - r) & (capacity - 1);
        }
    }

    public int AvailableToWrite => capacity - AvailableToRead - 1;

    /// <summary>
    /// Writes as much as possible. Returns bytes written.
    /// Drops data if insufficient space.
    /// </summary>
    public int Write(ReadOnlySpan<byte> src)
    {
        int available = AvailableToWrite;
        if (available == 0)
            return 0;

        int toWrite = Math.Min(src.Length, available);

        int w = writePos & (capacity - 1);
        int first = Math.Min(toWrite, capacity - w);

        src[..first].CopyTo(buffer.AsSpan(w));
        src[first..toWrite].CopyTo(buffer);

        Volatile.Write(ref writePos, writePos + toWrite);
        return toWrite;
    }

    /// <summary>
    /// Reads exactly dst.Length bytes if available.
    /// Returns false if insufficient data.
    /// </summary>
    public bool Read(Span<byte> dst)
    {
        if (AvailableToRead < dst.Length)
            return false;

        int r = readPos & (capacity - 1);
        int first = Math.Min(dst.Length, capacity - r);

        buffer.AsSpan(r, first).CopyTo(dst);
        buffer.AsSpan(0, dst.Length - first).CopyTo(dst[first..]);

        Volatile.Write(ref readPos, readPos + dst.Length);
        return true;
    }

    public void Reset()
    {
        Volatile.Write(ref readPos, 0);
        Volatile.Write(ref writePos, 0);
    }
}

#endif