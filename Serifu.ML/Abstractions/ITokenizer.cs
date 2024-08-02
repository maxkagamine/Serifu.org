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

public interface ITokenizer
{
    /// <summary>
    /// Splits text into words for alignment.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The start and end of each word as a collection of <see cref="Token"/>.</returns>
    IEnumerable<Token> Tokenize(string text);

    /// <summary>
    /// Gets the number of words in <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    int GetWordCount(string text) => Tokenize(text).Count();
}
