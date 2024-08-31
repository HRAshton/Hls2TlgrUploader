using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc cref="IFfmpegService" />
public partial class FfmpegService
{
    private static partial class Log
    {
        [LoggerMessage(LogLevel.Debug, "Disposing ffmpeg service")]
        public static partial void DisposingFfmpegService(ILogger logger);

        [LoggerMessage(LogLevel.Debug, "Ffmpeg service disposed")]
        public static partial void FfmpegServiceDisposed(ILogger logger);

        [LoggerMessage(LogLevel.Debug, "Starting ffmpeg instance with target file: {TargetFile}")]
        public static partial void StartingFfmpegInstance(ILogger logger, string targetFile);

        [LoggerMessage(LogLevel.Debug, "Ffmpeg process exited with code {ExitCode}")]
        public static partial void FfmpegProcessExited(ILogger logger, int exitCode);

        [LoggerMessage(LogLevel.Debug, "stderr: {StderrOutput}")]
        public static partial void FfmpegStderrOutput(ILogger logger, string? stderrOutput);
    }
}
