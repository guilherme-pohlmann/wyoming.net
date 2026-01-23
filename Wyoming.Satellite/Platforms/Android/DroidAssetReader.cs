using Android.Content.Res;
using System.Buffers;
using Wyoming.Net.Core;

namespace Wyoming.Net.Satellite;

public static class DroidAssetReader
{
    private const int Kb512 = 512000;

    public static byte[] ReadAsset(AssetManager assetManager, string path)
    {
        ArgumentNullException.ThrowIfNull(assetManager);

        try
        {
            return ReadUncompressed(assetManager, path);
        }
        catch
        {
            return ReadCompressed(assetManager, path);
        }
    }

    private static byte[] ReadCompressed(AssetManager assetManager, string path)
    {
        // Shouldn't be a hot path so not super optimized
        var fileBytes = new List<byte>(Kb512);
        var buffer = ArrayPool<byte>.Shared.Rent(Kb512);

        var file = assetManager!.Open(path, Access.Streaming);

        while (true)
        {
            int read = file.ReadAtLeast(buffer, buffer.Length, false);

            if (read == 0) break;

            fileBytes.AddRange(buffer.AsSpan().Slice(0, read));
        }

        return fileBytes.ToArray();
    }

    private static byte[] ReadUncompressed(AssetManager assetManager, string path)
    {
        // OpenFd requires the asset to be uncompressed on the APK.
        using AssetFileDescriptor afd = assetManager.OpenFd(path);
        long length = afd.Length;

        if (length > int.MaxValue)
        {
            throw new InvalidOperationException("Asset is too large to load into a single byte array.");
        }

        byte[] data = new byte[length];
        int offset = 0;

        using var stream = afd.CreateInputStream() ?? throw new InvalidOperationException("Failed to open file stream");

        while (offset < length)
        {
            int read = stream.Read(data, offset, (int)(length - offset));

            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        Asserts.IsTrue(offset == length, "Offset and length don't match");

        return data;
    }
}
