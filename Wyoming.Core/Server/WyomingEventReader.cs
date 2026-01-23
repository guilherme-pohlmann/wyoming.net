using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Wyoming.Net.Core.Events;
using static Wyoming.Net.Core.Server.ProtocolConstants;

namespace Wyoming.Net.Core.Server;

internal sealed class WyomingEventReader
{
    private readonly WyomingStreamReader streamReader;
    private readonly ILogger logger;

    public WyomingEventReader(WyomingStreamReader streamReader, ILogger logger)
    {
        this.streamReader = streamReader;
        this.logger = logger;
    }

    public async Task<Event?> ReadEventAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? line = await streamReader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(line))
            {
                logger.LogDebug("ReadLine returned null line");
                return null;
            }

            var header = TryDeserialize(line);

            if (header == null)
            {
                logger.LogDebug("Failed to deserialized line - {line}", line);
                return null;
            }

            Dictionary<string, object>? additionalData = null;

            if (header.TryGetValue(DataLength, out var dl) && dl is int dataLength and > 0)
            {
                additionalData = await ReadAdditionalDataAsync(dataLength, cancellationToken);

                if (additionalData == null)
                {
                    logger.LogDebug("ReadAdditionalData returned null. DataLength was {dataLength}", dataLength);
                }
            }

            byte[]? payload = null;

            if (header.TryGetValue(PayloadLength, out var pl) && pl is int payloadLength and > 0)
            {
                payload = await ReadPayloadDataAsync(payloadLength,  cancellationToken);
            }

            return new Event((string)header[ProtocolConstants.Type], MergeData(header.GetValueOrDefault(Data) as Dictionary<string, object>, additionalData), payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read event");
            return null;
        }
        finally
        {
            streamReader.Reset();
        }
    }

    private Dictionary<string, object>? TryDeserialize(ReadOnlySpan<char> json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializationOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Header] - Failed to deserialize event. Raw: {line}", json.ToString());
        }

        return null;
    }

    private Dictionary<string, object>? TryDeserialize(ReadOnlySpan<byte> json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializationOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Data] - Failed to deserialize event. Raw: {line}", Encoding.UTF8.GetString(json));
        }

        return null;
    }

    private static ReadOnlyDictionary<string, object> MergeData(IDictionary<string, object>? data, IDictionary<string, object>? additionalData)
    {
        if (data == null)
        {
            return additionalData?.AsReadOnly() ?? ReadOnlyDictionary<string, object>.Empty;
        }

        if (additionalData == null)
        {
            return data?.AsReadOnly() ?? ReadOnlyDictionary<string, object>.Empty;
        }

        foreach (var kv in additionalData)
        {
            if (!data.ContainsKey(kv.Key))
            {
                data.Add(kv.Key, kv.Value);
            }
        }

        return data.AsReadOnly();
    }

    private async Task<Dictionary<string, object>?> ReadAdditionalDataAsync(int dataLength, CancellationToken cancellationToken)
    {
        var dataBuffer = ArrayPool<byte>.Shared.Rent(dataLength);
        var memory = new Memory<byte>(dataBuffer, 0, dataLength);

        try
        {
            if (await streamReader.ReadExactlyAsync(memory, cancellationToken))
            {
                return TryDeserialize(memory.Span);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(dataBuffer);
        }

        return null;
    }

    private async Task<byte[]?> ReadPayloadDataAsync(int payloadLength, CancellationToken cancellationToken)
    {
        var payloadBuffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        var memory = new Memory<byte>(payloadBuffer, 0, payloadLength);

        try
        {
            if (await streamReader.ReadExactlyAsync(memory, cancellationToken))
            {
                return memory.ToArray();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payloadBuffer);
        }

        return null;
    }
}
