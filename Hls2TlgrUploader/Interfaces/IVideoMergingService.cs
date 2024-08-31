using System.IO.Pipelines;

namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Downloads HLS parts and writes them to a pipe.
/// </summary>
public interface IVideoMergingService
{
    /// <summary>
    /// Downloads HLS parts and writes them to a pipe.
    /// </summary>
    /// <param name="hlsParts">List of HLS parts.</param>
    /// <param name="pipeWriter">Pipe writer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task.</returns>
    Task DownloadToPipeAsync(IList<Uri> hlsParts, PipeWriter pipeWriter, CancellationToken cancellationToken);
}
