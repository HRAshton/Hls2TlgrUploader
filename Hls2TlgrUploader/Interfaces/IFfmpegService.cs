using System.IO.Pipelines;
using Hls2TlgrUploader.Models;

namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Creates and handles an instance of ffmpeg process.
/// Processes the input stream in real-time and writes the output to a file.
/// Disposes the process and the file when done.
/// </summary>
public interface IFfmpegService : IDisposable
{
    /// <summary>
    /// Gets the pipe writer to write data to the FFmpeg service.
    /// </summary>
    PipeWriter PipeWriter { get; }

    /// <summary>
    /// Gets the result of the FFmpeg service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the converted video file.</returns>
    Task<VideoInfo> GetResultAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs the FFmpeg service in the background.
    /// </summary>
    /// <param name="fileId">Unique identifier of the file to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    void Run(string fileId, CancellationToken cancellationToken);
}
