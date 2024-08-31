namespace Hls2TlgrUploader.Models;

/// <summary>
/// Represents video file information and path to the temporary file.
/// </summary>
/// <param name="FilePath">Temporary file path.</param>
/// <param name="Width">Width of the video.</param>
/// <param name="Height">Height of the video.</param>
/// <param name="DurationSec">Duration of the video in seconds.</param>
public sealed record VideoInfo(string FilePath, int Width, int Height, int DurationSec);
