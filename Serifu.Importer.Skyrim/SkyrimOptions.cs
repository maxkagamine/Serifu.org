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

using System.ComponentModel.DataAnnotations;

namespace Serifu.Importer.Skyrim;

public class SkyrimOptions
{
    /// <summary>
    /// Absolute path to the game's Data directory.
    /// </summary>
    [Required]
    public string DataDirectory { get; set; } = "";

    /// <summary>
    /// Absolute paths to the archive(s) containing English voice files.
    /// </summary>
    [Required, MinLength(1)]
    public List<string> EnglishVoiceBsaPaths { get; set; } = [];

    /// <summary>
    /// Absolute paths to the archive(s) containing Japanese voice files.
    /// </summary>
    [Required, MinLength(1)]
    public List<string> JapaneseVoiceBsaPaths { get; set; } = [];

    /// <summary>
    /// A map of faction editor IDs to list of either NPC names (matches all NPCs in the faction with the exact English
    /// name) or FormKeys (for when the NPC may not exist in the faction by default) to prioritize over the rest of the
    /// faction when it is used as part of a non-negated GetInFaction condition.
    /// </summary>
    /// <remarks>
    /// When both <see cref="FactionOverrides"/> and <see cref="FactionVoiceTypeOverrides"/> produce matches, NPCs
    /// that match both overrides are used, unless none do, in which case the former takes priority.
    /// </remarks>
    public Dictionary<string, List<string>> FactionOverrides { get; set; } = [];

    /// <summary>
    /// A map of faction editor IDs to a list of voice types (case-insensitive) to prioritize over the rest of the
    /// faction when it is used as part of a non-negated GetInFaction condition.
    /// </summary>
    /// <remarks>
    /// When both <see cref="FactionOverrides"/> and <see cref="FactionVoiceTypeOverrides"/> produce matches, NPCs
    /// that match both overrides are used, unless none do, in which case the former takes priority.
    /// </remarks>
    public Dictionary<string, List<string>> FactionVoiceTypeOverrides { get; set; } = [];

    /// <summary>
    /// Paths to voice files in the English archive(s) that will be treated as if they don't exist, to prevent
    /// silent/broken audio from being used. Case-insensitive; slashes will be normalized.
    /// </summary>
    public List<string> ExcludedEnglishVoiceFiles { get; set; } = [];

    /// <summary>
    /// Paths to voice files in the Japanese archive(s) that will be treated as if they don't exist, to prevent
    /// silent/broken audio from being used. Case-insensitive; slashes will be normalized.
    /// </summary>
    public List<string> ExcludedJapaneseVoiceFiles { get; set; } = [];
}
