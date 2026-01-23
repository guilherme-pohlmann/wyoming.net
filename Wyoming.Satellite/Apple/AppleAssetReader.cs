#if IOS || MACCATALYST

namespace Wyoming.Net.Satellite.Apple;

internal static class AppleAssetReader
{
    public static async Task<byte[]> ReadAsync(string path)
    {
        await using var fileStream = await FileSystem.OpenAppPackageFileAsync(Path.Combine("Raw", path));
        using var memoryStream = new MemoryStream();
        
        await fileStream.CopyToAsync(memoryStream);
        
        return memoryStream.ToArray();
    }
}

#endif