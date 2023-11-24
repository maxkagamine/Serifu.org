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
/// Represents an audio file for a particular <see cref="Translation"/>.
/// </summary>
/// <remarks>
/// Multiple quotes may have the same audio <paramref name="Path"/> (undesirable, as it means we got differing
/// translations for the same voice line), and multiple <paramref name="OriginalName"/> may refer to the same <paramref
/// name="Path"/> as well if their contents were identical. Rather than manage a separate index of audio files, this
/// type is embedded in the <see cref="Translation"/> as an owned entity.
/// </remarks>
/// <param name="Path">The file path relative to the audio directory; in cloud storage, this is the object name.</param>
/// <param name="OriginalName">The original filename or url to identify already-downloaded/processed files.</param>
/// <param name="LastModified">The date the audio file was last imported.</param>
public record AudioFile(string Path, string? OriginalName, DateTime? LastModified);
