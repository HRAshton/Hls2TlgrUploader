using Hls2TlgrUploader.Factories;
using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Tests.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hls2TlgrUploader.Tests.Factories;

public class FfmpegFactoryTests
{
    [Fact]
    public void CreateFfmpegService_ShouldCreateFfmpegServiceSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FfmpegFactory>>();
        var mockFfmpegService = new Mock<IFfmpegService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFfmpegService)))
            .Returns(mockFfmpegService.Object);

        var factory = new FfmpegFactory(mockLogger.Object, mockServiceProvider.Object);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = factory.CreateFfmpegService(cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockFfmpegService.Object, result);
        mockFfmpegService.Verify(s => s.Run(It.IsAny<string>(), cancellationToken), Times.Once);
    }

    [Fact]
    public void CreateFfmpegService_ShouldLogAndThrow_WhenServiceCreationFails()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FfmpegFactory>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var exception = new InvalidOperationException("Service not registered");
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFfmpegService)))
            .Throws(exception);

        var factory = new FfmpegFactory(mockLogger.Object, mockServiceProvider.Object);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateFfmpegService(cancellationToken));
        Assert.Equal(exception, ex);
    }

    [Fact]
    public void CreateFfmpegService_ShouldLogAndThrow_WhenRunFails()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FfmpegFactory>>();
        var mockFfmpegService = new Mock<IFfmpegService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runException = new TestException("Run failed");
        mockFfmpegService
            .Setup(s => s.Run(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(runException);

        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFfmpegService)))
            .Returns(mockFfmpegService.Object);

        var factory = new FfmpegFactory(mockLogger.Object, mockServiceProvider.Object);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var ex = Assert.Throws<TestException>(() => factory.CreateFfmpegService(cancellationToken));
        Assert.Equal(runException, ex);
    }
}
