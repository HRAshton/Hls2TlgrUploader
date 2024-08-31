using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hls2TlgrUploader.Factories;

/// <inheritdoc />
public sealed partial class FfmpegFactory(
    ILogger<FfmpegFactory> logger,
    IServiceProvider serviceProvider)
    : IFfmpegFactory
{
    private ILogger<FfmpegFactory> Logger => logger;

    private IServiceProvider ServiceProvider => serviceProvider;

    /// <inheritdoc />
    public IFfmpegService CreateFfmpegService(CancellationToken cancellationToken)
    {
        try
        {
            IFfmpegService ffmpegService = ServiceProvider.GetRequiredService<IFfmpegService>();
            ffmpegService.Run(GenerateUniqueId(), cancellationToken);
            Log.FfmpegServiceCreated(Logger);
            return ffmpegService;
        }
        catch (Exception ex)
        {
            Log.FailedToCreateOrRunFfmpegService(Logger, ex.Message, ex.StackTrace);
            throw;
        }
    }

    private static string GenerateUniqueId()
    {
        return Guid.NewGuid().ToString();
    }
}
