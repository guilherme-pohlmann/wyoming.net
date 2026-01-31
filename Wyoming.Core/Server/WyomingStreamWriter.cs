using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Wyoming.Net.Core.Events;

namespace Wyoming.Net.Core.Server;

public sealed class WyomingStreamWriter
{
    private readonly NetworkStream stream;

    public WyomingStreamWriter(NetworkStream stream)
    {
        this.stream = stream;
    }

    public async Task WriteEventAsync(Event @event)
    {
        //TODO: we can probably reduce allocations here, maybe zero?
        string data = string.Empty;

        if (@event.Data != null)
        {
            data = JsonSerializer.Serialize(@event.Data, ProtocolConstants.SerializationOptions);
        }

        int dataSize = Encoding.UTF8.GetByteCount(data);
        string header = $"{{\"{ProtocolConstants.Type}\":\"{@event.Type}\",\"{ProtocolConstants.DataLength}\":{dataSize},\"{ProtocolConstants.PayloadLength}\":{@event.Payload?.Length ?? 0}}}";
        
        WriteString(header);
        stream.WriteByte((byte)'\n');

        if (!string.IsNullOrEmpty(data))
        {
            WriteString(data, dataSize);
        }

        if(@event.Payload != null)
        {
            stream.Write(@event.Payload);
        }

        await stream.FlushAsync();
    }

    private void WriteString(string data, int? size = null)
    {
        Span<byte> buffer = stackalloc byte[size ?? Encoding.UTF8.GetByteCount(data)];
        Encoding.UTF8.GetBytes(data, buffer);
        stream.Write(buffer);
    }
}
