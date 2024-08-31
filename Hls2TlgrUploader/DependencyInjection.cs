using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Helpers;
using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using FfmpegFactory = Hls2TlgrUploader.Factories.FfmpegFactory;
using FfmpegService = Hls2TlgrUploader.Services.FfmpegService;
using VideoMergingService = Hls2TlgrUploader.Services.VideoMergingService;
using VideoUploadingService = Hls2TlgrUploader.Services.VideoUploadingService;

namespace Hls2TlgrUploader;

#pragma warning disable CA1724 // Type names should not match namespaces

/// <summary>
/// Dependency injection configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration.</param>
    /// <returns>Returns service collection.</returns>
    public static IServiceCollection AddHls2TlgrUploader(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .Configure<ProcessingConfig>(configuration.GetSection(ProcessingConfig.SectionName))
            .Configure<TelegramConfig>(configuration.GetSection(TelegramConfig.SectionName))
            .AddSingleton<ISemiConcurrentProcessingHelper, SemiConcurrentProcessingHelper>()
            .AddSingleton<IVideoUploadingService, VideoUploadingService>()
            .AddSingleton<IVideoMergingService, VideoMergingService>()
            .AddSingleton<IFfmpegFactory, FfmpegFactory>()
            .AddSingleton<IProcessWrapper, ProcessWrapper>()
            .AddSingleton<IIoHelper, IoHelper>()
            .AddTransient<IFfmpegService, FfmpegService>()
            .AddSingleton<ITelegramBotClient, TelegramBotClient>(provider =>
            {
                TelegramConfig telegramConfig = provider.GetRequiredService<IOptions<TelegramConfig>>().Value;
                var clientOptions = new TelegramBotClientOptions(
                    telegramConfig.BotToken,
                    telegramConfig.ApiUrl.ToString());
                var defaultClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(telegramConfig.TimeoutSeconds),
                };

                return new TelegramBotClient(clientOptions, defaultClient);
            });
    }
}
