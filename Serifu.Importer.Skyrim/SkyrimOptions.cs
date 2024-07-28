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
    public required string DataDirectory { get; set; }

    /// <summary>
    /// Absolute path to the archive containing English voice files.
    /// </summary>
    public required string EnglishVoiceBsaPath { get; set; }

    /// <summary>
    /// Absolute path to the archive containing Japanese voice files.
    /// </summary>
    public required string JapaneseVoiceBsaPath { get; set; }

    /// <summary>
    /// A map of faction editor IDs to list of either NPC names (matches all NPCs in the faction with the exact English
    /// name) or FormKeys (for when the NPC may not exist in the faction by default) to prioritize over the rest of the
    /// faction when it is used as part of a non-negated GetInFaction condition.
    /// </summary>
    public Dictionary<string, List<string>> FactionOverrides { get; set; } = [];
}
