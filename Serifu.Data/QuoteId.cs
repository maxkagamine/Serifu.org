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

namespace Serifu.Data;

/// <summary>
/// Static methods for generating stable quote IDs. The resulting ID is eight bytes combining the <see cref="Source"/>
/// with a source-specific series of bits. This allows for efficient updates compared to GUIDs when re-importing a
/// source's quotes, and can act as an implicit sort order when filtering to a single source.
/// </summary>
/// <remarks>
/// ID format:
/// <code>
///   [ Ship #      ] [ Index       ] [ Source ]  // Kancolle
///   [ Form ID                     ] [ Source ]  // Skyrim, Oblivion
/// </code>
/// Example: 21474836225 → Skyrim form id 04FFFFFF
/// </remarks>
public static class QuoteId
{
    // We could get fancy here and make a struct that casts to long and has properties to extract the encapsulated data,
    // which would be useful if we were passing IDs around or needed to validate IDs passed to an API etc., but it's not
    // really needed here since we're just saving them to the db as opaque IDs.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long CreateKancolleId(int shipNumber, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shipNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shipNumber, 0xFFFF);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 0xFFFF);

        return (shipNumber << 24) | (index << 8) | (byte)Source.Kancolle;
    }
}
