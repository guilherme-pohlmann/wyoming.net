using System.Collections.ObjectModel;
using System.Text.Json;
using Wyoming.Net.Core.Serialization;

namespace Wyoming.Net.Core.Server;

internal sealed record ProtocolConstants
{
    public static readonly JsonSerializerOptions SerializationOptions = new()
    {
        Converters = { new NestedDictionaryConverter() },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public const string DataLength = "data_length";

    public const string PayloadLength = "payload_length";

    public const string Type = "type";

    public const string Data = "data";
}
