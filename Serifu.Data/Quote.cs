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

using Kagamine.Extensions.Collections;

namespace Serifu.Data;

public record Quote
{
    /// <summary>
    /// ID generated by <see cref="QuoteId"/> that will refer to this quote even if the db is cleared and reimported.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// The game from which this quote originates, and the importer responsible for it.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// The quote's English translation.
    /// </summary>
    public required Translation English { get; init; }

    /// <summary>
    /// The quote's Japanese translation.
    /// </summary>
    public required Translation Japanese { get; init; }

    /// <summary>
    /// Word alignment data mapping from English to Japanese.
    /// </summary>
    public required ValueArray<Alignment> AlignmentData { get; init; }

    /// <summary>
    /// The date this quote was imported. May be used to determine if quotes need to be updated.
    /// </summary>
    public DateTime DateImported { get; init; } = DateTime.Now;

    /// <summary>
    /// A weight applied to the quote when building the Elasticsearch index to ensure that sources with a large number
    /// of quotes don't dominate the search results.
    /// </summary>
    /// <remarks>
    /// This property will be set automatically and is not stored in sqlite.
    /// </remarks>
    public double Weight { get; init; }
}
