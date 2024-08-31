using Hls2TlgrUploader.Interfaces;

namespace Hls2TlgrUploader.Helpers;

/// <inheritdoc />
public class SemiConcurrentProcessingHelper : ISemiConcurrentProcessingHelper
{
    private enum Status
    {
        Waiting,
        ProcessedByFirst,
        ProcessedBySecond,
    }

    /// <inheritdoc />
    public async Task Process<TListItem, TFirstCallbackResult>(
        IList<TListItem> items,
        Func<TListItem, int, int, CancellationToken, Task<TFirstCallbackResult>> concurrentCallback,
        Func<TFirstCallbackResult, int, int, CancellationToken, Task> ordinalCallback,
        int concurrency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));
        ArgumentNullException.ThrowIfNull(concurrentCallback, nameof(concurrentCallback));
        ArgumentNullException.ThrowIfNull(ordinalCallback, nameof(ordinalCallback));
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrency, 1);

        var allAreDifferent = items.Distinct().Count() == items.Count;
        if (!allAreDifferent)
        {
            throw new ArgumentException("All URLs should be different");
        }

        using var concurrentSemaphore = new SemaphoreSlim(concurrency, concurrency);
        using var ordinalSemaphore = new SemaphoreSlim(1, 1);

        Status[] statuses = items.Select(_ => Status.Waiting).ToArray();
        TFirstCallbackResult?[] results = items.Select(_ => default(TFirstCallbackResult)).ToArray();

        await Parallel.ForEachAsync(
            items.AsParallel().AsOrdered(),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = concurrency,
                CancellationToken = cancellationToken,
            },
            async (item, ct) =>
            {
                await ProcessItemAsync(item, ct);
                _ = StartProcessItemsOrdinal(ordinalCallback, ordinalSemaphore, statuses, results, ct);
            });

        // Process the remaining items.
        await StartProcessItemsOrdinal(ordinalCallback, ordinalSemaphore, statuses, results, cancellationToken);
        if (statuses.Any(s => s != Status.ProcessedBySecond))
        {
            throw new InvalidOperationException("Not all items were processed");
        }

        return;

        async Task ProcessItemAsync(TListItem item, CancellationToken ct)
        {
            await concurrentSemaphore.WaitAsync(ct);

            try
            {
                var index = items.IndexOf(item);
                results[index] = await concurrentCallback(item, index, items.Count, ct);
                statuses[index] = Status.ProcessedByFirst;
            }
            finally
            {
                _ = concurrentSemaphore.Release();
            }
        }
    }

    private static async Task StartProcessItemsOrdinal<TFirstCallbackResult>(
        Func<TFirstCallbackResult, int, int, CancellationToken, Task> ordinalCallback,
        SemaphoreSlim ordinalSemaphore,
        IList<Status> statuses,
        IList<TFirstCallbackResult?> results,
        CancellationToken cancellationToken)
    {
        await ordinalSemaphore.WaitAsync(cancellationToken);

        try
        {
            for (var i = 0; i < statuses.Count; i++)
            {
                switch (statuses[i])
                {
                    case Status.Waiting:
                        return;
                    case Status.ProcessedBySecond:
                        continue;
                    case Status.ProcessedByFirst:
                        // As we are processing in order, we can safely assume that the previous items are processed.
                        await ordinalCallback(results[i]!, i, statuses.Count, cancellationToken);
                        statuses[i] = Status.ProcessedBySecond;
                        results[i] = default;
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(statuses));
                }
            }
        }
        finally
        {
            _ = ordinalSemaphore.Release();
        }
    }
}
