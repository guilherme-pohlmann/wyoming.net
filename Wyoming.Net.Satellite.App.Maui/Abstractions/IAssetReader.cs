namespace Wyoming.Net.Satellite.App.Maui.Abstractions;

public interface IAssetReader
{
    Task<byte[]> ReadBytesAsync(string path);
}
