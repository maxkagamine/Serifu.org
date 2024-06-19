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

using System.Collections.Immutable;

namespace Serifu.Data;

/// <summary>
/// An audio file for a particular <see cref="Translation"/>, as stored in the sqlite database. In production <see
/// cref="Translation.AudioFile"/> will refer to an object in S3.
/// </summary>
/// <remarks>
/// This entity includes properties necessary for <a href="https://sqlite.org/sqlar.html">sqlar</a> compatibility. This
/// enables use of not only the sqlite3 CLI's archive options for extracting files, but more importantly <a
/// href="https://github.com/maxkagamine/sqlarserver">sqlarserver</a> for serving the sqlite database as a drop-in
/// replacement for S3 in dev.
/// </remarks>
public record AudioFile
{
    /// <summary>
    /// The audio file's object name.
    /// </summary>
    public required string ObjectName { get; init; }

    /// <summary>
    /// Unix file mode, required for sqlar compatibility.
    /// </summary>
    /// <returns>
    /// A file mode indicating a regular file with 0777 permissions.
    /// </returns>
    public int Mode { get; private set; } = 0x81ff;

    /// <summary>
    /// The date the audio file was imported.
    /// </summary>
    public DateTime DateImported { get; init; } = DateTime.Now;

    /// <summary>
    /// The length of <see cref="Data"/>, required for sqlar compatibility. This table will never store compressed data.
    /// </summary>
    public int Size { get => Data.Length; private set { } }

    /// <summary>
    /// The audio file data.
    /// </summary>
    public required ImmutableArray<byte> Data { get; init; }
}
