// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

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
    /// Tries to download the audio file specified in <see cref="VoiceLine.AudioFile"/>.
    /// </summary>
    /// <param name="voiceLine">The voice line whose audio file to download.</param>
    /// <param name="overwrite">Whether to replace the file if it exists.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="HttpRequestException"></exception>
    public async Task DownloadAudioFile(VoiceLine voiceLine, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var filename = voiceLine.AudioFile;

        if (filename is null)
        {
            return;
        }

        var dir = Path.Combine(AudioDirectory, voiceLine.SpeakerEnglish);
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
