using Hls2TlgrUploader.Interfaces;

namespace Hls2TlgrUploader.Helpers;

/// <inheritdoc />
public class IoHelper : IIoHelper
{
    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        _ = Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}
