namespace Hls2TlgrUploader.Configuration;

/// <summary>
/// Configuration for Telegram.
/// </summary>
public sealed record TelegramConfig
{
    /// <summary>
    /// Name of the section in the configuration file.
    /// </summary>
    public const string SectionName = "Telegram";

    /// <summary>
    /// Gets telegram bot token.
    /// </summary>
    public required string BotToken { get; init; }

    /// <summary>
    /// Gets identifier of the destination chat.
    /// </summary>
    public required string DestinationChatId { get; init; }

    /// <summary>
    /// Gets api URL.
    /// Default value: https://api.telegram.org.
    /// </summary>
    public Uri ApiUrl { get; init; } = new("https://api.telegram.org");

    /// <summary>
    /// Gets timeout of the whole processing in seconds.
    /// Default value: 600 secs (10 minutes).
    /// </summary>
    public int TimeoutSeconds { get; init; } = 10 * 60;
}
