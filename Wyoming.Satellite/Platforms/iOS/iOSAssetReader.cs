namespace Wyoming.Net.Satellite;

public static class iOSAssetReader
{
    public static Task<byte[]> ReadAsync(string path)
    {
        return Apple.AppleAssetReader.ReadAsync(path);
    }
}