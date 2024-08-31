using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Models;
using Microsoft.Extensions.Logging;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc cref="IVideoUploadingService" />
public partial class VideoUploadingService
{
    private static partial class Log
    {
        [LoggerMessage(
            LogLevel.Debug,
            "Started copying video with {HlsPartsCount} parts")]
        public static partial void StartedCopying(ILogger logger, int hlsPartsCount);

        [LoggerMessage(LogLevel.Debug, "Processing video {SignedUrl}")]
        public static partial void ProcessingVideo(ILogger logger, Uri signedUrl);

        [LoggerMessage(LogLevel.Debug, "Got video info: {VideoInfo}")]
        public static partial void GotVideoInfo(ILogger logger, VideoInfo videoInfo);

        [LoggerMessage(LogLevel.Debug, "Thumbnail fetched")]
        public static partial void ThumbnailFetched(ILogger logger);

        [LoggerMessage(LogLevel.Information, "Video uploaded with message id: {MessageId}")]
        public static partial void VideoUploaded(ILogger logger, int messageId);

        [LoggerMessage(LogLevel.Debug, "Video uploaded")]
        public static partial void VideoUploaded(ILogger logger);
    }
}
