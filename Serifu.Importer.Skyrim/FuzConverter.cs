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

using FFMpegCore;
using Kagamine.Extensions.IO;
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
    private readonly ITemporaryFileProvider tempFileProvider;

    public FuzConverter(ILogger logger, ITemporaryFileProvider tempFileProvider)
    {
        this.tempFileProvider = tempFileProvider;
        this.logger = logger.ForContext<FuzConverter>();
    }

    public async Task<Stream> ConvertToOpus(Stream fuzStream, CancellationToken cancellationToken = default)
    {
        using TemporaryFile xwmTempFile = tempFileProvider.Create(".xwm");
        await using (var xwmStream = xwmTempFile.OpenWrite())
        {
            SeekToXwm(fuzStream);
            await fuzStream.CopyToAsync(xwmStream, cancellationToken);
        }

        using TemporaryFile opusTempFile = tempFileProvider.Create(".opus");

        var command = FFMpegArguments
            .FromFileInput(xwmTempFile.Path)
            .OutputToFile(opusTempFile.Path, true, options => options
                .WithAudioCodec("libopus")
                .WithAudioBitrate(Bitrate)
                .WithoutMetadata())
            .CancellableThrough(cancellationToken);

        logger.Debug("Running {Command}", "ffmpeg " + command.Arguments);
        await command.ProcessAsynchronously();

        return opusTempFile.OpenRead(deleteWhenClosed: true);
    }

    /// <summary>
    /// Seeks <paramref name="fuzStream"/> to the start of the XWM data.
    /// </summary>
    /// <param name="fuzStream">The fuz file stream.</param>
    /// <exception cref="UnsupportedAudioFormatException"/>
    private static void SeekToXwm(Stream fuzStream)
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
}
