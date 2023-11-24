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

using System.Diagnostics;

namespace Serifu.Data;

[DebuggerDisplay("Language = {Language}, Text = {Text}")]
public class Translation
{
    /// <summary>
    /// Two-letter language code.
    /// </summary>
    public required string Language { get; set; }

    /// <summary>
    /// The localized name of the character to whom this quote belongs. May be empty if unknown or generic.
    /// </summary>
    public required string SpeakerName { get; set; }

    /// <summary>
    /// A short, translated description of when this quote is spoken. May be empty if unknown or generic.
    /// </summary>
    public required string Context { get; set; }

    /// <summary>
    /// The translated quote.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Translation notes, if any. Contains sanitized HTML.
    /// </summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// The audio file path, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public string? AudioFile { get; set; }

    /// <summary>
    /// The original audio file. Source-specific.
    /// </summary>
    public string? OriginalAudioFile { get; set; }

    /// <summary>
    /// The date the audio file was imported/downloaded. Can be compared to the original's modified date
    /// (source-specific) to determine if the audio file needs to be updated.
    /// </summary>
    public DateTime? DateAudioFileImported { get; set; }
}
