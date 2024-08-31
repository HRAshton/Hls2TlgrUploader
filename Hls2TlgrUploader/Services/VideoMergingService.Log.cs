using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc cref="IVideoMergingService" />
public partial class VideoMergingService
{
    private static partial class Log
    {
        [LoggerMessage(LogLevel.Debug, "Started merging {TotalParts} parts")]
        public static partial void StartedMerging(ILogger logger, int totalParts);

        [LoggerMessage(LogLevel.Trace, "Downloaded part {PartNumber}/{TotalParts}")]
        public static partial void DownloadedPart(ILogger logger, int partNumber, int totalParts);

        [LoggerMessage(LogLevel.Trace, "Processed part {PartNumber}/{TotalParts}")]
        public static partial void ProcessedPart(ILogger logger, int partNumber, int totalParts);

        [LoggerMessage(LogLevel.Debug, "Finished merging")]
        public static partial void FinishedMerging(ILogger logger);
    }
}
