using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Wyoming.Net.Core.Server;

internal sealed class WyomingStreamReader : IDisposable
{
    private const int EOF = -1;

    private const int BlockSize = 1024;
    private readonly NetworkStream stream;
    private readonly byte[] readBuffer = new byte[BlockSize];
    private readonly ILogger<WyomingStreamReader> logger;

    private byte[] writeBuffer = null!;
    private int writePos;
    private int readPos;
    private int available;

    public WyomingStreamReader(NetworkStream stream, ILogger<WyomingStreamReader> logger)
    {
        this.stream = stream;
        this.logger = logger;
        Reset();
    }

    public void Reset()
    {
        if (available <= 0)
        {
            Debug("[Reset] - Not Available");

            if (writeBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(writeBuffer);
            }

            writePos = 0;
            readPos = 0;
            available = 0;
            writeBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);
        }
        else
        {
            Debug("[Reset] - Still Available");

            writeBuffer.AsSpan().Clear();
            writePos = 0;
        }
    }

    public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        bool found = false;
        Debug("[ReadLineAsync] Starting");

        while (!found)
        {
            if (available == 0)
            {
                Debug("[ReadLineAsync] Buffering", false);

                await BufferContentAsync(cancellationToken);

                Debug("[ReadLineAsync] Buffered");

                if (!CanFitAvailable())
                {
                    Grow();
                }
            }
            if (available == EOF)
            {
                break;
            }

            int totalBytes = available;

            for (int i = 0; i < totalBytes; i++)
            {
                available--;

                if ((char)readBuffer[readPos] == '\n')
                {
                    readPos++;
                    found = true;
                    Debug("[ReadLineAsync] Found");
                    break;
                }

                writeBuffer[writePos] = readBuffer[readPos];

                writePos++;
                readPos++;
            }
        }

        string line = Encoding.UTF8.GetString(writeBuffer.AsSpan(0, writePos));
        writePos = 0;
        available = Math.Max(available, 0);

        return line;
    }

    private void Debug(string message, bool dumpStats = true)
    {
        if(!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

#pragma warning disable CA2254 // Template should be a static expression
        logger.LogDebug(message);
#pragma warning restore CA2254 // Template should be a static expression

        if (dumpStats)
        {
            logger.LogDebug("ReadPos: {readPos} - WritePos: {writePos} - Available: {available}", readPos, writePos, available);
        }
    }

    public async Task<bool> ReadExactlyAsync(Memory<byte> memory, CancellationToken cancellationToken)
    {
        int filled = 0;
        int toFill = memory.Length;
        Debug("[ReadExactlyAsync] Starting", false);
        Debug("[ReadExactlyAsync] ToFill: " + toFill);

        while (filled < toFill)
        {
            if (available == 0)
            {
                Debug("[ReadExactlyAsync] Buffering", false);
                await BufferContentAsync(cancellationToken);

                Debug("[ReadExactlyAsync] Buffered");
            }

            if (available == EOF)
            {
                return false;
            }

            int chunk = Math.Min(available, toFill - filled);

            readBuffer.AsSpan(readPos, chunk)
                      .CopyTo(memory.Span.Slice(filled, chunk));

            available -= chunk;
            readPos += chunk;

            filled += chunk;

            Debug("[ReadExactlyAsync] Filled: " + filled, false);
            Debug("[ReadExactlyAsync] ToFill: " + toFill);
        }

        Debug("[ReadExactlyAsync] Done");
        return true;
    }

    private async Task BufferContentAsync(CancellationToken cancellationToken)
    {
        int read = await stream.ReadAsync(readBuffer.AsMemory(), cancellationToken);

        if (read == 0)
        {
            available = EOF;
            return;
        }

        available = read;
        readPos = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    private bool CanFitAvailable() => available <= writeBuffer.Length - writePos;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow()
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(writeBuffer.Length * 4);
        writeBuffer.CopyTo(newBuffer, 0);

        ArrayPool<byte>.Shared.Return(writeBuffer);

        writeBuffer = newBuffer;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(writeBuffer);
    }
}