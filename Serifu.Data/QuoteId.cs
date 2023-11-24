using System.Runtime.CompilerServices;

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
        ArgumentOutOfRangeException.ThrowIfNegative(shipNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shipNumber, 0xFFFF);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 0xFFFF);

        return ((long)Source.Kancolle << 32) | ((uint)(ushort)shipNumber << 16) | (ushort)index;
    }
}
