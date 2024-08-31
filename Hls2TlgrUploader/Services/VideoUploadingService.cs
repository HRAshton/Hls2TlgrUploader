using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc />
public sealed partial class VideoUploadingService(
    ILogger<VideoUploadingService> logger,
    IOptions<TelegramConfig> telegramConfigOptions,
    IFfmpegFactory ffmpegFactory,
    ITelegramBotClient telegramClient,
    IVideoMergingService videoMergingService,
    IIoHelper ioHelper)
    : IVideoUploadingService
{
    private TelegramConfig TelegramConfig => telegramConfigOptions.Value;

    private ILogger<VideoUploadingService> Logger => logger;

    private IFfmpegFactory FfmpegFactory => ffmpegFactory;

    private IVideoMergingService VideoMergingService => videoMergingService;

    private ITelegramBotClient TelegramClient => telegramClient;

    private IIoHelper IoHelper => ioHelper;

    /// <inheritdoc />
    public async Task<Message> CopyToTelegramAsync(
        IList<Uri> hlsParts,
        Task<Stream> jpegThumbnailStreamTask,
        string? caption,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hlsParts);
        ArgumentNullException.ThrowIfNull(jpegThumbnailStreamTask);

        Log.StartedCopying(Logger, hlsParts.Count);

        using IFfmpegService ffmpegService = FfmpegFactory.CreateFfmpegService(cancellationToken);
        VideoInfo videoInfo = await DownloadAndMergeVideoAsync(hlsParts, ffmpegService, cancellationToken);
        Log.GotVideoInfo(Logger, videoInfo);

        await using Stream thumbnailStream = await jpegThumbnailStreamTask;
        Log.ThumbnailFetched(Logger);

        Message message = await UploadVideoAsync(videoInfo, thumbnailStream, caption, cancellationToken);
        Log.VideoUploaded(Logger, message.MessageId);

        return message;
    }

    private async Task<VideoInfo> DownloadAndMergeVideoAsync(
        IList<Uri> hlsParts,
        IFfmpegService ffmpegService,
        CancellationToken cancellationToken)
    {
        await VideoMergingService.DownloadToPipeAsync(hlsParts, ffmpegService.PipeWriter, cancellationToken);

        await ffmpegService.PipeWriter.CompleteAsync();
        VideoInfo videoInfo = await ffmpegService.GetResultAsync(cancellationToken);

        return videoInfo;
    }

    private async Task<Message> UploadVideoAsync(
        VideoInfo videoInfo,
        Stream thumbnailStream,
        string? caption,
        CancellationToken cancellationToken)
    {
        await using Stream fileStream = IoHelper.OpenRead(videoInfo.FilePath);

        Message message = await TelegramClient.SendVideoAsync(
            TelegramConfig.DestinationChatId,
            new InputFileStream(fileStream),
            caption: caption,
            width: videoInfo.Width,
            height: videoInfo.Height,
            duration: videoInfo.DurationSec,
            thumbnail: new InputFileStream(thumbnailStream),
            supportsStreaming: true,
            cancellationToken: cancellationToken);
        Log.VideoUploaded(Logger);

        return message;
    }
}
