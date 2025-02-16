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
using Serilog;

namespace Serifu.S3Uploader;

/// <summary>
/// Safari has abysmal support for standard audio formats. Unfortunately, we can't just ignore iPhone users, but I also
/// don't want to force lower quality / larger file size AAC upon everyone else just because Apple can't get with the
/// times, so that means we either have to serve fallback M4A files for Safari users, or try bundling a WASM audio
/// decoder. As the only Apple device I have to test with at the moment is my friend's iPhone, the latter isn't really a
/// viable option. The lossy->lossy conversion here is unfortunate, but refactoring everything to handle multiple audio
/// formats per translation and then reimporting all the things is a headache that I don't need right now. Blame Apple
/// for not supporting open audio codecs and containers.
/// </summary>
public class AacAudioFallbackConverter
{
    private readonly ILogger logger;
    private readonly ITemporaryFileProvider tempFileProvider;

    public AacAudioFallbackConverter(ILogger logger, ITemporaryFileProvider tempFileProvider)
    {
        this.logger = logger;
        this.tempFileProvider = tempFileProvider;
    }

    public async Task<Stream> ConvertToAac(Stream inputStream, CancellationToken cancellationToken = default)
    {
        using TemporaryFile inputFile = tempFileProvider.Create();
        await inputFile.CopyFromAsync(inputStream, cancellationToken);

        using TemporaryFile outputFile = tempFileProvider.Create(".m4a");

        // This requires an ffmpeg build compiled with libfdk-aac. See:
        //   https://trac.ffmpeg.org/wiki/Encode/AAC
        //   https://github.com/FT129/Handbrake-and-FFmpeg-with-fdk-aac (Windows builds)
        var command = FFMpegArguments
            .FromFileInput(inputFile.Path)
            .OutputToFile(outputFile.Path, true, options => options
                .WithAudioCodec("libfdk_aac")
                .WithCustomArgument("-profile:a aac_he")
                .WithVariableBitrate(3)
                .WithCustomArgument("-movflags +faststart"))
            .CancellableThrough(cancellationToken);

        logger.Debug("Running {Command}", "ffmpeg " + command.Arguments);
        await command.ProcessAsynchronously();

        return outputFile.OpenRead(deleteWhenClosed: true);
    }
}
