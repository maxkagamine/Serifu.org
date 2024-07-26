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

namespace Serifu.Importer.Skyrim;

public class SkyrimOptions
{
    /// <summary>
    /// Absolute path to the game's Data directory.
    /// </summary>
    public required string DataDirectory { get; init; }

    /// <summary>
    /// Absolute path to the archive containing English voice files.
    /// </summary>
    public required string EnglishVoiceBsaPath { get; init; }

    /// <summary>
    /// Absolute path to the archive containing Japanese voice files.
    /// </summary>
    public required string JapaneseVoiceBsaPath { get; init; }
}
