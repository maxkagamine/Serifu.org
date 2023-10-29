using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serifu.Data.Entities;

namespace Serifu.Importer.Kancolle;
internal class AudioFileProcessor : IProcessor
{
    const string FileRedirectBaseUrl = "https://en.kancollewiki.net/Special:Redirect/file/";

    private readonly Channel<AudioFileQueueItem> audioFileQueue;
    private readonly HttpClient httpClient;
    private readonly ILogger<AudioFileProcessor> logger;
    private readonly string audioDirectory;

    public AudioFileProcessor(
        Channel<AudioFileQueueItem> audioFileQueue,
        HttpClient httpClient,
        ILogger<AudioFileProcessor> logger,
        IConfiguration configuration)
    {
        this.audioFileQueue = audioFileQueue;
        this.httpClient = httpClient;
        this.logger = logger;

        string audioDirectoryRoot = configuration["AudioDirectory"]
            ?? throw new Exception("Configuration missing AudioDirectory.");
        audioDirectory = Path.Combine(audioDirectoryRoot, nameof(Source.Kancolle));

        logger.LogInformation("Kancolle audio directory is {AudioDirectory}", Path.GetFullPath(audioDirectory));
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        await foreach (var queueItem in audioFileQueue.Reader.ReadAllAsync(cancellationToken))
        {
            var quote = queueItem.Quote;
            var filename = quote.AudioFile;

            if (filename is null)
            {
                continue;
            }

            var filePath = Path.Combine(audioDirectory, quote.SpeakerEnglish, filename);

            if (File.Exists(filePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            logger.LogInformation("Downloading {Filename}", filename);

            try
            {
                var tempPath = Path.GetTempFileName();
                using (var stream = await httpClient.GetStreamAsync(FileRedirectBaseUrl + filename, cancellationToken))
                using (var file = File.OpenWrite(tempPath))
                {
                    await stream.CopyToAsync(file, cancellationToken);
                }

                File.Move(tempPath, filePath);

                logger.LogInformation("Downloaded {Filename}", filename);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("{Ship}'s audio file {AudioFile} returned 404.", quote.SpeakerEnglish, filename);
            }
        }
    }
}
