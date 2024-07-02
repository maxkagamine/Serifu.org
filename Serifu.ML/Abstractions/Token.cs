namespace Serifu.ML.Abstractions;

/// <summary>
/// Represents a token by its start and end index.
/// </summary>
/// <remarks>
/// This type provides an advantage over <see cref="Range"/> in that its offsets can be destructured and accessed
/// directly instead of needing to call <see cref="Index.GetOffset(int)"/> with the original text length (as an <see
/// cref="Index"/> can be from the end).
/// </remarks>
/// <param name="Start">The inclusive start index.</param>
/// <param name="End">The exclusive end index.</param>
public readonly record struct Token(ushort Start, ushort End)
{
    public Token(int start, int end) : this(checked((ushort)start), checked((ushort)end))
    { }

    public static implicit operator Range(Token token) => new(token.Start, token.End);
}