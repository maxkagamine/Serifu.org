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

using Serifu.ML.Tokenizers;

namespace Serifu.ML.Tests;

public class TokenizerTests
{
    [Theory]
    [InlineData("I'm the light cruiser, Tama. I'm not a cat-nya.", new[] { "I'm", "the", "light", "cruiser", "Tama", "I'm", "not", "a", "cat", "nya" })]
    [InlineData("CarDiv3, flagship Zuihou, setting sail!", new[] { "CarDiv", "3", "flagship", "Zuihou", "setting", "sail" })]
    [InlineData("I'm Hibi-- Verniy. It's a name that means \"Faithful\".", new[] { "I'm", "Hibi", "Verniy", "It's", "a", "name", "that", "means", "Faithful" })]
    [InlineData("Foo’s 'wouldn't've' this/that; sueño $9,000.00.", new[] { "Foo’s", "wouldn't've", "this", "that", "sueño", "9,000.00" })]
    public void EnglishTokenizer(string text, string[] expected)
    {
        EnglishTokenizer tokenizer = new();

        Range[] ranges = tokenizer.Tokenize(text).ToArray();
        string[] actual = ranges.Select(r => text[r]).ToArray();

        Assert.Equal(expected, actual);

        // Make sure they're in incrementing order, so e.g. the second "I'm" in Tama's line isn't the range for the first "I'm"
        Assert.All(ranges, (range, i) => Assert.True(i == 0 || range.Start.GetOffset(text.Length) >= ranges[i - 1].End.GetOffset(text.Length)));
    }
}
