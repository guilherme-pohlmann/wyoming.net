using Wyoming.Net.Satellite.App.Maui;

namespace Wyoming.Net.Satellite.App.Maui.Abstractions;

internal sealed class AssetReader : IAssetReader
{
    public Task<byte[]> ReadBytesAsync(string path)
    {
#if ANDROID
        return Task.FromResult(DroidAssetReader.ReadAsset(Android.App.Application.Context.Assets!, path));
#endif
        
#if IOS
        return iOSAssetReader.ReadAsync(path);
#endif
        
#if MACCATALYST
        return MacAssetReader.ReadAsync(path);
#endif
        
        throw new NotSupportedException();
    }
}
