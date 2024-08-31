namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Wrapper for the I/O operations.
/// </summary>
public interface IIoHelper
{
    /// <summary>
    /// Opens a file for writing.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Opens a file for writing.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>Stream for writing to the file.</returns>
    Stream OpenRead(string path);
}
