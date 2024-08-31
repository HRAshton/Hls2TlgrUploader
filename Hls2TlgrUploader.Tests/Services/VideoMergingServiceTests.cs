using System.IO.Pipelines;
using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Hls2TlgrUploader.Tests.Services;

public class VideoMergingServiceTests
{
    private readonly Mock<ILogger<VideoMergingService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<ProcessingConfig>> _processingConfigMock;
    private readonly Mock<ISemiConcurrentProcessingHelper> _semiConcurrentProcessingHelper;

    private readonly ProcessingConfig _processingConfig;

    public VideoMergingServiceTests()
    {
        _loggerMock = new Mock<ILogger<VideoMergingService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _processingConfigMock = new Mock<IOptions<ProcessingConfig>>();
        _semiConcurrentProcessingHelper = new Mock<ISemiConcurrentProcessingHelper>();

        _processingConfig = new ProcessingConfig
        {
            ConcurrentDownloads = 3, // Example concurrent downloads setting
        };
        _processingConfigMock
            .Setup(c => c.Value)
            .Returns(_processingConfig);
    }

    [Fact]
    public async Task DownloadToPipeAsync_SuccessfulDownloadAndPipe()
    {
        // Arrange
        var service = new VideoMergingService(
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _processingConfigMock.Object,
            _semiConcurrentProcessingHelper.Object);

        List<Uri> hlsParts =
        [
            new("https://example.com/video1.ts"),
            new("https://example.com/video2.ts"),
        ];
        var pipe = new Pipe();
        var cancellationToken = CancellationToken.None;

        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClientMock.Object);

        _semiConcurrentProcessingHelper
            .Setup(p => p.Process(
                It.IsAny<IList<Uri>>(),
                It.IsAny<Func<Uri, int, int, CancellationToken, Task<byte[]>>>(),
                It.IsAny<Func<byte[], int, int, CancellationToken, Task>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.DownloadToPipeAsync(hlsParts, pipe.Writer, cancellationToken);

        // Assert
        _semiConcurrentProcessingHelper.Verify(
            p => p.Process(
                hlsParts,
                It.IsAny<Func<Uri, int, int, CancellationToken, Task<byte[]>>>(),
                It.IsAny<Func<byte[], int, int, CancellationToken, Task>>(),
                _processingConfig.ConcurrentDownloads,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task DownloadToPipeAsync_NullHlsParts_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new VideoMergingService(
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _processingConfigMock.Object,
            _semiConcurrentProcessingHelper.Object);

        IList<Uri> hlsParts = null!;
        var pipe = new Pipe();
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.DownloadToPipeAsync(hlsParts, pipe.Writer, cancellationToken));
    }

    [Fact]
    public async Task DownloadToPipeAsync_MultipleParts_ProcessesAllParts()
    {
        // Arrange
        var service = new VideoMergingService(
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _processingConfigMock.Object,
            _semiConcurrentProcessingHelper.Object);

        List<Uri> hlsParts =
        [
            new("https://example.com/video1.ts"),
            new("https://example.com/video2.ts"),
        ];
        var pipe = new Pipe();
        var cancellationToken = CancellationToken.None;

        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

        _semiConcurrentProcessingHelper
            .Setup(p => p.Process(
                hlsParts,
                It.IsAny<Func<Uri, int, int, CancellationToken, Task<byte[]>>>(),
                It.IsAny<Func<byte[], int, int, CancellationToken, Task>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.DownloadToPipeAsync(hlsParts, pipe.Writer, cancellationToken);

        // Assert
        _semiConcurrentProcessingHelper
            .Verify(
                p => p.Process(
                    hlsParts,
                    It.IsAny<Func<Uri, int, int, CancellationToken, Task<byte[]>>>(),
                    It.IsAny<Func<byte[], int, int, CancellationToken, Task>>(),
                    _processingConfig.ConcurrentDownloads,
                    cancellationToken),
                Times.Once);
    }
}
