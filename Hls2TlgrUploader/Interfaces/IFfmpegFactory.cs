namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Factory for the <see cref="IFfmpegService"/>.
/// </summary>
public interface IFfmpegFactory
{
    /// <summary>
    /// Creates a new instance of the <see cref="IFfmpegService"/>.
    /// The instance must be disposed after usage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of the <see cref="IFfmpegService"/>.</returns>
    IFfmpegService CreateFfmpegService(CancellationToken cancellationToken);
}
