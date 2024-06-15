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

        return ((long)shipNumber << 24) | ((long)index << 8) | (byte)Source.Kancolle;
    }
}
