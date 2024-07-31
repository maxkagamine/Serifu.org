using FFMpegCore.Exceptions;
using Serifu.Data.Sqlite;

namespace Serifu.Importer.Skyrim;

public interface IFuzConverter
{
    /// <summary>
    /// Extracts the XWM data from <paramref name="fuzStream"/> and converts it to Opus using ffmpeg.
    /// </summary>
    /// <param name="fuzStream">The fuz stream.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The opus stream.</returns>
    /// <exception cref="UnsupportedAudioFormatException"/>
    /// <exception cref="FFMpegException"/>
    Task<Stream> ConvertToOpus(Stream fuzStream, CancellationToken cancellationToken = default);
}
