using FFMpegCore;
using Microsoft.Extensions.Hosting;
using Noggog.IO;
using Serifu.Data.Sqlite;
using Serilog;
using System.Buffers.Binary;

namespace Serifu.Importer.Skyrim;

public class FuzConverter : IFuzConverter
{
    // 32 kbps is a rather low bitrate, but the vanilla voice files are around 35 kbps to begin with. We could probe the
    // source bitrate and bump this up to a more respectable 96k for the English voices since we're using the UHDAP, but
    // there's very little difference, especially compared to UHDAP vs. the vanilla trash audio. Opus is way better at
    // low-bitrate speech than xWMA (go figure).
    private const int Bitrate = 32;

    private readonly ILogger logger;
    private readonly IHostEnvironment hostEnv;

    public FuzConverter(ILogger logger, IHostEnvironment hostEnv)
    {
        this.logger = logger.ForContext<FuzConverter>();
        this.hostEnv = hostEnv;
    }

    public async Task<Stream> ConvertToOpus(Stream fuzStream, CancellationToken cancellationToken = default)
    {
        using var xwmTempFile = new TempFile(hostEnv.ApplicationName, suffix: ".xwm");
        using (var xwmStream = File.OpenWrite(xwmTempFile.File))
        {
            SeekToXwm(fuzStream);
            await fuzStream.CopyToAsync(xwmStream, cancellationToken);
        }

        var opusTempFile = new TempFile(hostEnv.ApplicationName, suffix: ".opus");

        // Expects ffmpeg in PATH (install with winget)
        var command = FFMpegArguments
            .FromFileInput(xwmTempFile.File)
            .OutputToFile(opusTempFile.File, true, options => options
                .WithAudioCodec("libopus")
                .WithAudioBitrate(Bitrate)
                .WithoutMetadata())
            .CancellableThrough(cancellationToken);

        logger.Debug("Running {Command}", "ffmpeg " + command.Arguments);
        await command.ProcessAsynchronously();

        // This stream will delete the temp file when disposed
        return new TempFileStream(opusTempFile, FileMode.Open, FileAccess.Read);
    }

    /// <summary>
    /// Seeks <paramref name="fuzStream"/> to the start of the XWM data.
    /// </summary>
    /// <param name="fuzStream">The fuz file stream.</param>
    /// <exception cref="UnsupportedAudioFormatException"/>
    private void SeekToXwm(Stream fuzStream)
    {
        // Offset  Length   Purpose
        // ------  -------  -----------------------------------------
        // 00      4 bytes  Magic header "FUZE" (0x46 0x55 0x5A 0x45)
        // 04      4 bytes  Version number (little endian, always 1)
        // 08      4 bytes  Lip data size (may be zero if no lip)
        // *       *        Lip data
        // *       *        Xwm data

        Span<byte> header = stackalloc byte[12];
        fuzStream.ReadExactly(header);

        int fuze = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (fuze != 0x455A5546)
        {
            throw new UnsupportedAudioFormatException("Not a FUZE file");
        }

        int version = BinaryPrimitives.ReadInt32LittleEndian(header[4..8]);
        if (version != 1)
        {
            throw new UnsupportedAudioFormatException($"Unsupported FUZE version: {version} (expected 1)");
        }

        int lipSize = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
        fuzStream.Seek(lipSize, SeekOrigin.Current);
    }

    private class TempFileStream(TempFile tempFile, FileMode mode, FileAccess access) : FileStream(tempFile.File, mode, access)
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            tempFile.Dispose();
        }
    }
}
