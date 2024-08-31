using System.IO.Pipelines;
using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Models;
using Hls2TlgrUploader.Services;
using Hls2TlgrUploader.Tests.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Hls2TlgrUploader.Tests.Services;

public class VideoUploadingServiceTests
{
    private readonly Mock<ILogger<VideoUploadingService>> _loggerMock;
    private readonly Mock<IOptions<TelegramConfig>> _configMock;
    private readonly Mock<IFfmpegFactory> _ffmpegFactoryMock;
    private readonly Mock<ITelegramBotClient> _telegramClientMock;
    private readonly Mock<IVideoMergingService> _videoMergingServiceMock;
    private readonly Mock<IFfmpegService> _ffmpegServiceMock;
    private readonly Mock<IIoHelper> _ioHelperMock;

    public VideoUploadingServiceTests()
    {
        _loggerMock = new Mock<ILogger<VideoUploadingService>>();
        _configMock = new Mock<IOptions<TelegramConfig>>();
        _ffmpegFactoryMock = new Mock<IFfmpegFactory>();
        _telegramClientMock = new Mock<ITelegramBotClient>();
        _videoMergingServiceMock = new Mock<IVideoMergingService>();
        _ffmpegServiceMock = new Mock<IFfmpegService>();
        _ioHelperMock = new Mock<IIoHelper>();

        _ffmpegServiceMock
            .SetupGet(f => f.PipeWriter)
            .Returns(new Mock<PipeWriter>().Object);
        _ffmpegServiceMock
            .Setup(f => f.GetResultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoInfo("test.mp4", 1280, 720, 60));

        _ffmpegFactoryMock
            .Setup(f => f.CreateFfmpegService(It.IsAny<CancellationToken>()))
            .Returns(() => _ffmpegServiceMock.Object);

        var telegramConfig = new TelegramConfig
        {
            BotToken = "someToken",
            DestinationChatId = "@someChatId",
        };
        _configMock.Setup(c => c.Value).Returns(telegramConfig);
    }

    [Fact]
    public async Task CopyToTelegramAsync_SuccessfulUpload_ReturnsMessageId()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        var hlsParts = new List<Uri>
        {
            new("https://example.com/video1.m3u8"),
        };
        var thumbnailStream = new MemoryStream();
        var thumbnailStreamTask = Task.FromResult<Stream>(thumbnailStream);
        var cancellationToken = CancellationToken.None;

        _telegramClientMock
            .Setup(t => t.MakeRequestAsync(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message
            {
                MessageId = 1,
            });

        var videoStream = new MemoryStream();
        _ioHelperMock
            .Setup(i => i.OpenRead(It.IsAny<string>()))
            .Returns(videoStream);

        // Act
        var result = await service.CopyToTelegramAsync(
            hlsParts,
            thumbnailStreamTask,
            "Test Caption",
            cancellationToken);

        // Assert
        Assert.Equal(1, result.MessageId);
        _telegramClientMock
            .Verify(t => t.MakeRequestAsync(
                    It.Is<IRequest<Message>>(request =>
                        ((SendVideoRequest)request).Caption == "Test Caption"
                        && ((SendVideoRequest)request).ChatId == "@someChatId"
                        && ((SendVideoRequest)request).DisableNotification == null
                        && ((SendVideoRequest)request).Duration == 60
                        && ((SendVideoRequest)request).HasSpoiler == null
                        && ((SendVideoRequest)request).Height == 720
                        && ((SendVideoRequest)request).MessageThreadId == null
                        && ((SendVideoRequest)request).MethodName == "sendVideo"
                        && ((SendVideoRequest)request).ParseMode == null
                        && ((SendVideoRequest)request).ProtectContent == null
                        && ((SendVideoRequest)request).ReplyMarkup == null
                        && ((SendVideoRequest)request).ReplyToMessageId == null
                        && ((SendVideoRequest)request).SupportsStreaming == true
                        && ((SendVideoRequest)request).Thumbnail != null
                        && ((SendVideoRequest)request).Thumbnail!.FileType == FileType.Stream
                        && ((InputFileStream)((SendVideoRequest)request).Thumbnail!).Content == thumbnailStream
                        && ((InputFileStream)((SendVideoRequest)request).Thumbnail!).FileName == null
                        && ((InputFileStream)((SendVideoRequest)request).Video).Content == videoStream
                        && ((InputFileStream)((SendVideoRequest)request).Video).FileName == null
                        && ((SendVideoRequest)request).Width == 1280),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task CopyToTelegramAsync_NullHlsParts_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        IList<Uri> hlsParts = null!;
        var thumbnailStreamTask = Task.FromResult<Stream>(new MemoryStream());
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CopyToTelegramAsync(hlsParts, thumbnailStreamTask, "Test Caption", cancellationToken));
    }

    [Fact]
    public async Task CopyToTelegramAsync_NullJpegThumbnailStreamTask_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        List<Uri> hlsParts = [ new("https://example.com/video1.m3u8") ];
        Task<Stream> thumbnailStreamTask = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CopyToTelegramAsync(hlsParts, thumbnailStreamTask, "Test Caption", cancellationToken));
    }

    [Fact]
    public async Task CopyToTelegramAsync_DownloadAndMergeFails_ThrowsException()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        List<Uri> hlsParts = [ new("https://example.com/video1.m3u8") ];
        var thumbnailStreamTask = Task.FromResult<Stream>(new MemoryStream());
        var cancellationToken = CancellationToken.None;

        var ffmpegServiceMock = new Mock<IFfmpegService>();
        _ffmpegFactoryMock.Setup(f => f.CreateFfmpegService(cancellationToken)).Returns(ffmpegServiceMock.Object);

        _videoMergingServiceMock.Setup(v => v.DownloadToPipeAsync(hlsParts, It.IsAny<PipeWriter>(), cancellationToken))
            .ThrowsAsync(new TestException("Download failed"));

        // Act & Assert
        await Assert.ThrowsAsync<TestException>(() =>
            service.CopyToTelegramAsync(hlsParts, thumbnailStreamTask, "Test Caption", cancellationToken));
    }

    [Fact]
    public async Task CopyToTelegramAsync_UploadFails_ThrowsException()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        List<Uri> hlsParts = [ new("https://example.com/video1.m3u8") ];
        var thumbnailStreamTask = Task.FromResult<Stream>(new MemoryStream());
        var cancellationToken = CancellationToken.None;

        _telegramClientMock
            .Setup(t => t.MakeRequestAsync(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestException("Upload failed"));

        // Act & Assert
        await Assert.ThrowsAsync<TestException>(() =>
            service.CopyToTelegramAsync(hlsParts, thumbnailStreamTask, "Test Caption", cancellationToken));
    }

    [Fact]
    public async Task CopyToTelegramAsync_DisposesFfmpegService()
    {
        // Arrange
        var service = new VideoUploadingService(
            _loggerMock.Object,
            _configMock.Object,
            _ffmpegFactoryMock.Object,
            _telegramClientMock.Object,
            _videoMergingServiceMock.Object,
            _ioHelperMock.Object);

        List<Uri> hlsParts = [ new("https://example.com/video1.m3u8") ];
        var thumbnailStreamTask = Task.FromResult<Stream>(new MemoryStream());
        var cancellationToken = CancellationToken.None;

        _telegramClientMock
            .Setup(t => t.MakeRequestAsync(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message
            {
                MessageId = 1,
            });

        // Act
        await service.CopyToTelegramAsync(hlsParts, thumbnailStreamTask, "Test Caption", cancellationToken);

        // Assert
        _ffmpegServiceMock.Verify(f => f.Dispose(), Times.Once);
    }
}
