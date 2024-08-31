using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hls2TlgrUploader.Factories;

/// <inheritdoc cref="IFfmpegFactory" />
public partial class FfmpegFactory
{
    private static partial class Log
    {
        [LoggerMessage(LogLevel.Debug, "Ffmpeg service created")]
        public static partial void FfmpegServiceCreated(ILogger logger);

        [LoggerMessage(
            LogLevel.Error,
            "Failed to create or run ffmpeg service: {ExceptionMessage}. {ExceptionStackTrace}")]
        public static partial void FailedToCreateOrRunFfmpegService(
            ILogger logger,
            string exceptionMessage,
            string? exceptionStackTrace);
    }
}
