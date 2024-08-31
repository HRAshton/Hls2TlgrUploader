namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Helper to process items concurrently and in order.
/// </summary>
public interface ISemiConcurrentProcessingHelper
{
    /// <summary>
    /// Downloads a list of items concurrently and processes them in order.
    /// </summary>
    /// <param name="items">Items to process.</param>
    /// <param name="concurrentCallback">Callback 1 to process each item concurrently.</param>
    /// <param name="ordinalCallback">Callback 2 to process each item in order.</param>
    /// <param name="concurrency">Number of concurrent tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TListItem">Type of the items to process.</typeparam>
    /// <typeparam name="TFirstCallbackResult">Type of the result of the 1st callback.</typeparam>
    /// <returns>Task.</returns>
    Task Process<TListItem, TFirstCallbackResult>(
        IList<TListItem> items,
        Func<TListItem, int, int, CancellationToken, Task<TFirstCallbackResult>> concurrentCallback,
        Func<TFirstCallbackResult, int, int, CancellationToken, Task> ordinalCallback,
        int concurrency,
        CancellationToken cancellationToken);
}
