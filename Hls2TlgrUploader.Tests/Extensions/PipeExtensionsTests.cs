using System.Buffers;
using System.IO.Pipelines;
using Hls2TlgrUploader.Extensions;
using Moq;

#pragma warning disable CS8604 // Possible null reference argument.

namespace Hls2TlgrUploader.Tests.Extensions;

public class PipeExtensionsTests
{
    [Fact]
    public async Task PumpToAsync_ThrowsArgumentNullException_WhenPipeReaderIsNull()
    {
        // Arrange
        PipeReader? pipeReader = null;
        var outputStream = new Mock<Stream>().Object;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => pipeReader.PumpToAsync(outputStream, cancellationToken));
    }

    [Fact]
    public async Task PumpToAsync_ThrowsArgumentNullException_WhenOutputStreamIsNull()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>().Object;
        Stream? outputStream = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => pipeReader.PumpToAsync(outputStream, cancellationToken));
    }

    [Fact(Timeout = 5000)]
    public async Task PumpToAsync_ThrowsOperationCanceledException_WhenCancellationTokenIsAlreadyCanceled()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>().Object;
        var outputStream = new Mock<Stream>().Object;
        var cancellationToken = new CancellationToken(canceled: true);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipeReader.PumpToAsync(outputStream, cancellationToken));
    }

    [Fact]
    public async Task PumpToAsync_ThrowsOperationCanceledException_WhenCancellationTokenIsCanceledDuringOperation()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        pipeReader
            .SetupSequence(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadResult(default, isCanceled: false, isCompleted: false))
            .ReturnsAsync(new ReadResult(default, isCanceled: false, isCompleted: true));

        // Act
        await cts.CancelAsync();
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipeReader.Object.PumpToAsync(outputStream.Object, cancellationToken));

        // Assert
        Assert.IsType<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task PumpToAsync_CompletesImmediately_WhenPipeReaderReturnsCompletedResult()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();

        pipeReader
            .Setup(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadResult(default, isCanceled: false, isCompleted: true));

        // Act
        await pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None);

        // Assert
        outputStream.Verify(s => s.Close(), Times.Once);
        outputStream.Verify(
            s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PumpToAsync_WritesMultipleBuffersToOutputStream()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();

        var buffer = new ReadOnlySequence<byte>([ 1, 2, 3 ]);
        var readResult = new ReadResult(buffer, isCanceled: false, isCompleted: false);
        var completedResult = new ReadResult(buffer, isCanceled: false, isCompleted: true);

        pipeReader
            .SetupSequence(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResult)
            .ReturnsAsync(completedResult);

        // Act
        await pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None);

        // Assert
        outputStream.Verify(
            s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        outputStream.Verify(s => s.Close(), Times.Once);
    }

    [Fact]
    public async Task PumpToAsync_HandlesEmptyBufferCorrectly()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();

        var emptyBuffer = new ReadOnlySequence<byte>([ ]);
        var completedResult = new ReadResult(emptyBuffer, isCanceled: false, isCompleted: true);

        pipeReader
            .Setup(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedResult);

        // Act
        await pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None);

        // Assert
        outputStream.Verify(
            s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        outputStream.Verify(s => s.Close(), Times.Once);
    }

    [Fact]
    public async Task PumpToAsync_ThrowsException_WhenOutputStreamWriteAsyncFails()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();
        var buffer = new ReadOnlySequence<byte>([ 1, 2, 3 ]);
        var readResult = new ReadResult(buffer, isCanceled: false, isCompleted: false);

        pipeReader
            .Setup(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResult);

        outputStream
            .Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException());

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(
            () => pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None));
    }

    [Fact]
    public async Task PumpToAsync_SuccessfullyPumpsDataUntilCompleted()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();

        var buffer = new ReadOnlySequence<byte>([ 1, 2, 3 ]);
        var readResult = new ReadResult(buffer, isCanceled: false, isCompleted: false);
        var completedResult = new ReadResult(buffer, isCanceled: false, isCompleted: true);

        pipeReader
            .SetupSequence(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResult)
            .ReturnsAsync(completedResult);

        // Act
        await pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None);

        // Assert
        outputStream.Verify(
            s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        outputStream.Verify(s => s.Close(), Times.Once);
    }

    [Fact]
    public async Task PumpToAsync_ThrowsNotSupportedException_WhenOutputStreamDoesNotSupportWriting()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new MemoryStream([ ], writable: false);

        var buffer = new ReadOnlySequence<byte>([ 1, 2, 3 ]);
        var readResult = new ReadResult(buffer, isCanceled: false, isCompleted: false);
        var completedResult = new ReadResult(default, isCanceled: false, isCompleted: true);

        pipeReader
            .SetupSequence(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResult)
            .ReturnsAsync(completedResult);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => pipeReader.Object.PumpToAsync(outputStream, CancellationToken.None));
    }

    [Fact]
    public async Task PumpToAsync_HandlesException_WhenOutputStreamCloseFails()
    {
        // Arrange
        var pipeReader = new Mock<PipeReader>();
        var outputStream = new Mock<Stream>();

        var completedResult = new ReadResult(default, isCanceled: false, isCompleted: true);

        pipeReader
            .Setup(p => p.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedResult);

        outputStream
            .Setup(s => s.Close())
            .Throws(new IOException());

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(
            () => pipeReader.Object.PumpToAsync(outputStream.Object, CancellationToken.None));
    }
}
