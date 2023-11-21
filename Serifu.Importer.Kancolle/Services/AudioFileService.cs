using Serifu.Data.Entities;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles downloading audio files.
/// </summary>
internal class AudioFileService
{
    const string AudioDirectory = $"audio/{nameof(Source.Kancolle)}";
    const string FileRedirectBaseUrl = "https://en.kancollewiki.net/Special:Redirect/file/";

    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public AudioFileService(
        HttpClient httpClient,
        ILogger logger)
    {
        this.httpClient = httpClient;
        this.logger = logger.ForContext<AudioFileService>();

        logger.Information("Kancolle audio directory is {Path}", Path.GetFullPath(AudioDirectory));
    }

    /// <summary>
    /// Downloads the audio file specified in <see cref="Quote.AudioFile"/>.
    /// </summary>
    /// <param name="quote">The quote whose audio file to download.</param>
    /// <param name="overwrite">Whether to replace the file if it exists.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="HttpRequestException"></exception>
    public async Task DownloadAudioFile(Quote quote, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var filename = quote.AudioFile;

        if (filename is null)
        {
            return;
        }

        var dir = Path.Combine(AudioDirectory, quote.SpeakerEnglish);
        var filePath = Path.Combine(dir, filename);

        if (!overwrite && File.Exists(filePath))
        {
            logger.Debug("{File} already downloaded.", filename);
            return;
        }

        logger.Information("Downloading {File}", filename);

        var tempPath = Path.GetTempFileName();

        using (var stream = await httpClient.GetStreamAsync(FileRedirectBaseUrl + filename, cancellationToken))
        using (var file = File.OpenWrite(tempPath))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        Directory.CreateDirectory(dir);
        File.Move(tempPath, filePath, overwrite);
    }
}
