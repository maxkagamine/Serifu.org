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

using Serifu.ML.Abstractions;
using Serifu.ML.Tokenizers;

namespace Serifu.ML.Tests;

public sealed class TokenizerTests
{
    [Theory]
    [InlineData("I'm the light cruiser, Tama. I'm not a cat-nya.", new[] { "I'm", "the", "light", "cruiser", "Tama", "I'm", "not", "a", "cat", "nya" })]
    [InlineData("CarDiv3, flagship Zuihou, setting sail!", new[] { "CarDiv", "3", "flagship", "Zuihou", "setting", "sail" })]
    [InlineData("I'm Hibi-- Verniy. It's a name that means \"Faithful\".", new[] { "I'm", "Hibi", "Verniy", "It's", "a", "name", "that", "means", "Faithful" })]
    [InlineData("Foo’s 'wouldn't've' this/that; sueño $9,000.00.", new[] { "Foo’s", "wouldn't've", "this", "that", "sueño", "9,000.00" })]
    public void EnglishTokenizer(string text, string[] expected) =>
        Tokenizer(new EnglishTokenizer(), text, expected);

    [Theory]
    [InlineData("軽巡、多摩です。猫じゃないにゃ。", new[] { "軽巡", "多摩", "です", "猫", "じゃない", "にゃ" })]
    [InlineData("瑞鳳です。軽空母ですが、練度が上がれば、正規空母並の活躍をお見せできます。", new[] { "瑞鳳", "です", "軽", "空母", "です", "が", "練度", "が", "上がれば", "正規", "空母", "並", "の", "活躍", "を", "お", "見せできます" })]
    [InlineData("ひび…Верный だ。信頼できると言う意味の名なんだ", new[] { "ひび", "Верный", "だ", "信頼", "できる", "と", "言う", "意味", "の", "名", "な", "ん", "だ" })]
    [InlineData("彗星は彗星で悪くないんだけれど、整備大変なのよ、整備が。", new[] { "彗星", "は", "彗星", "で", "悪くない", "ん", "だ", "けれど", "整備", "大変", "な", "の", "よ", "整備", "が" })]
    [InlineData(
        // For whatever reason, MeCab thinks "!?" is a noun which messes up Ve
        "んっ、なっ……!? い……いや……き、嫌いでは……ない……", new[] { "んっ", "なっ", "い", "いや", "き", "嫌い", "で", "は", "ない" }
    )]
    [InlineData(
        // The よう in this sentence causes eatNext to be true which triggers a bug in Ve
        "あらあら、私に何かようなの？", new[] { "あらあら", "私", "に", "何", "か", "ような", "の" }
    )]
    [InlineData(
        // Just another one that caused parsing weirdness
        "補給( ・∀・)キタコレ！(ﾟдﾟ)ウマー！！", new[] { "補給", "キタコレ", "ﾟ", "д", "ﾟ", "ウマー" }
    )]
    public void JapaneseTokenizer(string text, string[] expected) =>
        Tokenizer(new JapaneseTokenizer(), text, expected);

    private static void Tokenizer(ITokenizer tokenizer, string text, string[] expected)
    {
        Token[] tokens = tokenizer.Tokenize(text).ToArray();
        string[] actual = tokens.Select(t => text[(Range)t]).ToArray();

        Assert.Equal(expected, actual);

        // Make sure they're in incrementing order, so e.g. the second "I'm" in Tama's line isn't the range for the first "I'm"
        Assert.All(tokens, (range, i) => Assert.True(i == 0 || range.Start >= tokens[i - 1].End));
    }
}
