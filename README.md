# Hls2TlgrUploader

This library uploads HLS stream to Telegram channel.

## How to use

1. Add nuget package
2. Set up your appsettings.json
3. Register services
4. Use `IVideoUploadingService`

### Add nuget package

```bash
dotnet add package Hls2TlgrUploader
```

### Set up your appsettings.json

```json
{
    "Telegram": {
        "BotToken": "YOUR_BOT_TOKEN",
        "DestinationChatId": "YOUR_CHAT_ID_OR_CHANNEL_ALIAS"
    }
}
```

### Register services

The package provides an extension method for `IServiceCollection`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHls2TlgrUploader(Configuration);
}
```

However, all necessary services are public, so you can register them manually.

### Use `IVideoUploadingService`

See also Hls2TlgrUploader.IntegrationTests.

```csharp
public class YourService
{
    private readonly IVideoUploadingService _videoUploadingService;

    public YourService(IVideoUploadingService videoUploadingService)
    {
        _videoUploadingService = videoUploadingService;
    }

    public async Task UploadVideoAsync()
    {
        Uri[] hlsParts = GetHlsPartsUrls();
        Task<Stream> thumbnailJpgStream = GetThumbnail();
        Message message = await service.CopyToTelegramAsync(
            uris,
            thumbnailJpgStream,
            "Some caption",
            CancellationToken.None);
    }
}
```

## Configuration

The package uses 2 configuration sections: `Telegram` and `Processing`.
You can pass a configuration object to `AddHls2TlgrUploader` method.

Only 2 properties are required: `Telegram:BotToken` and `Telegram:DestinationChatId`.

Available options:

- Telegram
  - BotToken (required)
  - DestinationChatId (required)
  - ApiUrl (default: https://api.telegram.org) - Telegram API URL
  - TimeoutSeconds (default: 60) - Timeout for Telegram API requests

- Processing
  - ConcurrentDownloads (default: 1) - Number of concurrent downloads. See [Download concurrency](#download-concurrency)
  - TempVideoFilePattern (default: "video-{0}.mp4") - Pattern for temporary video files.
    {0} is replaced with a random GUID in format "D" (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxx).
  - FfmpegPath (default: "ffmpeg") - Path to ffmpeg executable

## Download concurrency <a name="download-concurrency"></a>

The package can use `HttpClient` for downloading HLS parts.
The parts are downloaded into memory and processed as they arrive.
