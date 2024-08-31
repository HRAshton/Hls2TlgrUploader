using System.Globalization;
using Hls2TlgrUploader.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;

namespace Hls2TlgrUploader.IntegrationTests;

/// <summary>
/// Integration tests.
/// </summary>
public class Test
{
    /// <summary>
    /// Publish video and check the result.
    /// </summary>
    /// <returns>Returns task.</returns>
    [Fact]
    public async Task PublishVideo()
    {
        var uris = GetUris();
        var service = GetService();

        var message = await service.CopyToTelegramAsync(uris, GetThumbnail(), "Some caption", CancellationToken.None);

        Assert.NotNull(message);
        Assert.Equal("Some caption", message.Caption);
        Assert.True(message.MessageId > 0);
        Assert.Equal(message.Date, DateTime.Now.ToUniversalTime(), TimeSpan.FromMinutes(1));

        Assert.Equal(MessageType.Video, message.Type);
        Assert.NotNull(message.Video);
        Assert.NotNull(message.Video.FileId);
        Assert.Equal(12, message.Video.Duration);
        Assert.Equal("video", message.Video.FileName);
        Assert.Equal(1920, message.Video.Width);
        Assert.Equal(1080, message.Video.Height);
        Assert.Equal("video/mp4", message.Video.MimeType);

        Assert.NotNull(message.Video.Thumbnail);
        Assert.NotNull(message.Video.Thumbnail.FileId);
        Assert.Equal(300, message.Video.Thumbnail.Width);
        Assert.Equal(300, message.Video.Thumbnail.Height);
    }

    private static IVideoUploadingService GetService()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHls2TlgrUploader(configuration);
        serviceCollection.AddLogging();
        serviceCollection.AddHttpClient();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IVideoUploadingService>();
    }

    private static Uri[] GetUris()
    {
        const string baseUrl =
            "https://devstreaming-cdn.apple.com/videos/streaming/examples/img_bipbop_adv_example_ts/v9/fileSequence{0}.ts";

        return Enumerable.Range(0, 2)
            .Select(i => new Uri(string.Format(CultureInfo.InvariantCulture, baseUrl, i)))
            .ToArray();
    }

    private static async Task<Stream> GetThumbnail()
    {
        var jpgExample = new Uri("https://upload.wikimedia.org/wikipedia/en/a/a9/Example.jpg");
        var client = new HttpClient();
        var response = await client.GetAsync(jpgExample);
        return await response.Content.ReadAsStreamAsync();
    }
}
