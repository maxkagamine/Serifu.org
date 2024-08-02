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

using Serifu.Data;

namespace Serifu.ML.Abstractions;

public interface IWordAligner
{
    /// <summary>
    /// Uses machine learning to map words in the English text to words in the Japanese text. The model is run in both
    /// directions and the results combined.
    /// </summary>
    /// <param name="english">The English text.</param>
    /// <param name="japanese">The Japanese text.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The resulting alignments mapping from English to Japanese.</returns>
    Task<IEnumerable<Alignment>> AlignSymmetric(string english, string japanese, CancellationToken cancellationToken = default);

    /// <summary>
    /// The tokenizer used for English.
    /// </summary>
    ITokenizer EnglishTokenizer { get; }

    /// <summary>
    /// The tokenizer used for Japanese.
    /// </summary>
    ITokenizer JapaneseTokenizer { get; }
}
