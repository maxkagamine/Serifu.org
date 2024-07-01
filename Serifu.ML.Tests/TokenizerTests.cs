using Serifu.ML.Abstractions;
using Serifu.ML.Tokenizers;

namespace Serifu.ML.Tests;

public class TokenizerTests
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
    public void JapaneseTokenizer(string text, string[] expected) =>
        Tokenizer(new JapaneseTokenizer(), text, expected);

    private static void Tokenizer(ITokenizer tokenizer, string text, string[] expected)
    {
        Range[] ranges = tokenizer.Tokenize(text).ToArray();
        string[] actual = ranges.Select(r => text[r]).ToArray();

        Assert.Equal(expected, actual);

        // Make sure they're in incrementing order, so e.g. the second "I'm" in Tama's line isn't the range for the first "I'm"
        Assert.All(ranges, (range, i) => Assert.True(i == 0 || range.Start.GetOffset(text.Length) >= ranges[i - 1].End.GetOffset(text.Length)));
    }
}
