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

namespace Serifu.Data;

/// <summary>
/// Used to keep track of already-imported audio files.
/// </summary>
public record AudioFileCache
{
    /// <summary>
    /// The original audio file URL, if downloading from the web, or local file path, to avoid re-extracting/converting.
    /// </summary>
    /// <remarks>
    /// File URIs should be constructed as "file:///{Source}/{PathRelativeToGameDir}#{PathInArchive}".
    /// </remarks>
    public required Uri OriginalUri { get; init; }

    /// <summary>
    /// The already-imported <see cref="AudioFile.ObjectName"/>.
    /// </summary>
    public required string ObjectName { get; init; }
}
