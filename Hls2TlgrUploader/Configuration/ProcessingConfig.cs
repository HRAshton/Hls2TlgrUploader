namespace Hls2TlgrUploader.Configuration;

/// <summary>
/// Configuration for processing.
/// </summary>
public sealed record ProcessingConfig
{
    /// <summary>
    /// Section name in the configuration file.
    /// </summary>
    public const string SectionName = "Processing";

    /// <summary>
    /// Gets number of files to download concurrently.
    /// Default value: 1.
    /// </summary>
    public int ConcurrentDownloads { get; init; } = 1;

    /// <summary>
    /// Gets pattern for temporary video files. {0} is replaced with a guid.
    /// Default value: video-{0}.mp4.
    /// </summary>
    public string TempVideoFilePattern { get; init; } = "video-{0}.mp4";

    /// <summary>
    /// Gets path to ffmpeg.
    /// Default value: ffmpeg.
    /// </summary>
    public string FfmpegPath { get; init; } = "ffmpeg";
}
