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

public sealed record Translation
{
    /// <summary>
    /// The localized name of the character to whom this quote belongs. May be empty if unknown or generic.
    /// </summary>
    public required string SpeakerName { get; init; }

    /// <summary>
    /// A short, translated description of when this quote is spoken. May be empty if unknown or generic.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// The translated quote.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The number of words in <see cref="Text"/>.
    /// </summary>
    public required int WordCount { get; init; }

    /// <summary>
    /// Translation notes, if any. Contains sanitized HTML.
    /// </summary>
    public string Notes { get; init; } = "";

    /// <summary>
    /// The audio file object name, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public string? AudioFile { get; init; }
}
