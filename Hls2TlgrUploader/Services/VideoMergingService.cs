using System.IO.Pipelines;
using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Constants;
using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc />
public sealed partial class VideoMergingService(
    ILogger<VideoMergingService> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<ProcessingConfig> processingConfig,
    ISemiConcurrentProcessingHelper semiConcurrentProcessingHelper)
    : IVideoMergingService
{
    private ProcessingConfig ProcessingConfig => processingConfig.Value;

    private ILogger<VideoMergingService> Logger => logger;

    private IHttpClientFactory HttpClientFactory => httpClientFactory;

    private ISemiConcurrentProcessingHelper SemiConcurrentProcessingHelper => semiConcurrentProcessingHelper;

    /// <inheritdoc />
    public async Task DownloadToPipeAsync(
        IList<Uri> hlsParts,
        PipeWriter pipeWriter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hlsParts);

        Log.StartedMerging(Logger, hlsParts.Count);

        using HttpClient httpClient = HttpClientFactory.CreateClient(HttpClients.Hls2TlgrUploaderHttpClient);

        await SemiConcurrentProcessingHelper.Process(
            hlsParts,
            async (partUri, i, total, ct) => await DownloadPartAsync(partUri, i, total, httpClient, ct),
            async (partBytes, i, total, ct) => await PipePartAsync(partBytes, i, total, pipeWriter, ct),
            ProcessingConfig.ConcurrentDownloads,
            cancellationToken);

        Log.FinishedMerging(Logger);
    }

    private async Task<byte[]> DownloadPartAsync(
        Uri uri,
        int currentUrlIndex,
        int totalUrls,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetByteArrayAsync(uri, cancellationToken);
        Log.DownloadedPart(Logger, currentUrlIndex + 1, totalUrls);
        return response;
    }

    private async Task PipePartAsync(
        byte[] videoPartBytes,
        int currentPartIndex,
        int totalParts,
        PipeWriter pipeWriter,
        CancellationToken cancellationToken)
    {
        _ = await pipeWriter.WriteAsync(videoPartBytes, cancellationToken);
        Log.ProcessedPart(Logger, currentPartIndex + 1, totalParts);
    }
}
