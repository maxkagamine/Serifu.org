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