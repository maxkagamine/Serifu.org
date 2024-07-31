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
