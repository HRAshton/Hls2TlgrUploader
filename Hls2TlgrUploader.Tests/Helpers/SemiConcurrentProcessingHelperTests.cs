using Hls2TlgrUploader.Helpers;

namespace Hls2TlgrUploader.Tests.Helpers;

public class SemiConcurrentProcessingHelperTests
{
    private readonly SemiConcurrentProcessingHelper _semiConcurrentProcessingHelper = new();

    [Fact]
    public async Task Process_ShouldThrowArgumentException_WhenUrlsAreNotUnique()
    {
        // Arrange
        List<string> urls =
        [
            "https://example.com",
            "https://example.com",
        ];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _semiConcurrentProcessingHelper.Process(
                urls,
                (_, _, _, _) => Task.FromResult(Array.Empty<byte>()),
                (_, _, _, _) => Task.CompletedTask,
                1,
                CancellationToken.None));

        Assert.Equal("All URLs should be different", exception.Message);
    }

    /// <summary>
    /// Execution order:
    /// |10--------|
    /// |50------------------------------------------------|
    /// |30----------------------------|
    /// ...........|60----------------------------------------------------------|
    /// ...............................|21-------------------|
    /// ...................................................|11---------|
    /// .....................................................|61-----------------------------------------------------------|
    /// ...............................................................|32------------------------------|
    /// </summary>
    [Fact(Timeout = (116 * 10) + 10)] // Should be done in ~(116 * 10)ms
    public async Task Process_ShouldCallFirstCallbackConcurrently()
    {
        // Arrange
        List<int> items = [ 10, 50, 30, 60, 21, 11, 61, 32 ];
        var downloadCalls = new List<int>();

        // Act
        await _semiConcurrentProcessingHelper.Process(
            items,
            concurrentCallback: async (item, _, _, token) =>
            {
                await Task.Delay(item * 10, token); // Simulate delay
                downloadCalls.Add(item);
                return item;
            },
            ordinalCallback: (_, _, _, _) => Task.CompletedTask,
            concurrency: 3,
            CancellationToken.None);

        // Assert
        Assert.Equal(8, downloadCalls.Count);
        Assert.Equal([ 10, 30, 50, 21, 11, 60, 32, 61 ], downloadCalls);
    }

    [Fact]
    public async Task Process_ShouldCallSecondCallbackSequentially()
    {
        // Arrange
        List<int> items = [ 10, 50, 30, 60, 21, 11, 61, 32 ];
        var processCalls = new List<int>();

        // Act
        await _semiConcurrentProcessingHelper.Process(
            items,
            concurrentCallback: (item, _, _, _) => Task.FromResult(item), // Simulate the first callback
            ordinalCallback: (item, _, _, _) =>
            {
                // Capture the sequential processing order
                processCalls.Add(item);
                return Task.CompletedTask;
            },
            concurrency: 3,
            CancellationToken.None);

        // Assert
        Assert.Equal(8, processCalls.Count);
        Assert.Equal(items, processCalls);
    }
}
