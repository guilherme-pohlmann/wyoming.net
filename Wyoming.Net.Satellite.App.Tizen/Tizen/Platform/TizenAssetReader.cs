using System.IO;
using System.Threading.Tasks;

namespace Wyoming.Net.Satellite.App.Tz.Platform;

internal sealed class TizenAssetReader
{
    public static string ResourceDir => Tizen.Applications.Application.Current.DirectoryInfo.Resource;

    public static string DataDir => Tizen.Applications.Application.Current.DirectoryInfo.Data;

    public static Task<byte[]> ReadAssetAsync(string name)
    {
        return File.ReadAllBytesAsync(Path.Combine(ResourceDir, name));
    }

    public static string GetResourcePath(string name) => Path.Combine(ResourceDir, name);
}
