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

using System.Runtime.CompilerServices;
using Serifu.Data.Entities;

namespace Serifu.Data;

/// <summary>
/// Static methods for generating stable IDs for a given quote. The resulting ID is eight bytes (in practice only five
/// are used) combining the <see cref="Source"/> with a source-specific series of bits.
///
/// This approach allows for updating specific rows in Elasticsearch when re-importing an entire Source's quotes.
/// </summary>
/// <remarks>
/// ID format:
/// <code>
///   [      Source      ] [ Ship # ] [ Index ]  // Kancolle
///   [      Source      ] [ Form ID          ]  // Skyrim, Oblivion
/// </code>
/// Example: 4378853375 → Skyrim form id 04FFFFFF
/// </remarks>
public static class QuoteId
{
    // We could get fancy here and make a struct that casts to long and has properties to extract the encapsulated data,
    // which would be useful if we were passing IDs around or needed to validate IDs passed to an API etc., but it's not
    // really needed here since we're just saving them to the db as opaque IDs.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long CreateKancolleId(int shipNumber, int index)
    {
        return ((long)Source.Kancolle << 32) | ((uint)(ushort)shipNumber << 16) | (ushort)index;
    }
}