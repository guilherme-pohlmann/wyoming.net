using System;
using System.Runtime.InteropServices;

namespace Wyoming.Net.Satellite.App.Tz.Platform.Interop;

internal static class NativeAudio
{
    private const string LibMedia = "libcapi-media-audio-io.so.0";

    // So this hack is because the TizenFx API always allocate a new byte array
    // on every call to AudioCapture.Read(). We don't want that since
    // we are calling this many times per second and want to avoid GC pressure, 
    // so we PInvoke directly into the native API which takes a C-style buffer pointer as argument 
    // and allocates ourselves on the stack
    [DllImport(LibMedia, EntryPoint = "audio_in_read")]
    internal static extern AudioIOError Read(IntPtr handle, ref byte buffer, int length);

    // Kind of the same here but mainly to handle partial writes without having to reallocate
    // since this API doesn't take a start index
    [DllImport(LibMedia, EntryPoint = "audio_out_write")]
    internal static extern AudioIOError Write(IntPtr handle, ref byte buffer, uint length);
}