namespace Serifu.Data;

/// <summary>
/// A word alignment mapping a span of the "from" text to its translation in the "to" text.
/// </summary>
/// <param name="FromStart">The inclusive start index of the "from" text.</param>
/// <param name="FromEnd">The exclusive end index of the "from" text.</param>
/// <param name="ToStart">The inclusive start index of the "to" text.</param>
/// <param name="ToEnd">The exclusive end index of the "to" text.</param>
public readonly record struct Alignment(ushort FromStart, ushort FromEnd, ushort ToStart, ushort ToEnd);
