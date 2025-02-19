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

using Moq;
using Serifu.Data;
using Serifu.ML.Abstractions;
using Serilog;
using Xunit.Abstractions;

namespace Serifu.ML.Tests;

public sealed class WordAlignerTests : IDisposable
{
    private readonly Mock<IQuestionAnsweringPipeline> pipeline;
    private readonly WordAligner aligner;
    private readonly ILogger logger;

    public WordAlignerTests(ITestOutputHelper output)
    {
        pipeline = new Mock<IQuestionAnsweringPipeline>(MockBehavior.Strict);

        var transformers = Mock.Of<ITransformersContext>(x =>
            x.QuestionAnswering(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()) == Task.FromResult(pipeline.Object));

        logger = output.CreateTestLogger();
        aligner = new WordAligner(transformers, logger);
    }

    [Fact]
    public async Task IntegrationTest()
    {
        string englishText = "I'm Zuihou. Even though I'm a light carrier, I can show you that I'll be as good as standard carriers with some experience.";
        string japaneseText = "瑞鳳です。軽空母ですが、練度が上がれば、正規空母並の活躍をお見せできます。";

        var aligner = new WordAligner(new TransformersContext(logger), logger);

        IEnumerable<Alignment> result = await aligner.AlignSymmetric(englishText, japaneseText);

        logger.Information("Result: {Result}", string.Join(',', result.Select(x => $"{x.FromStart},{x.FromEnd},{x.ToStart},{x.ToEnd}")));
    }

    [Fact]
    public async Task AsksTheRightQuestions() // There's an I, Robot joke in here somewhere
    {
        string englishText = "I'm the light cruiser, Tama. I'm not a cat-nya.";
        string japaneseText = "軽巡、多摩です。猫じゃないにゃ。";

        // This test relies on the TokenizerTests passing
        string[] expectedEnglishQuestions = [
            " ¶ I'm ¶  the light cruiser, Tama. I'm not a cat-nya.",
            "I'm  ¶ the ¶  light cruiser, Tama. I'm not a cat-nya.",
            "I'm the  ¶ light ¶  cruiser, Tama. I'm not a cat-nya.",
            "I'm the light  ¶ cruiser ¶ , Tama. I'm not a cat-nya.",
            "I'm the light cruiser,  ¶ Tama ¶ . I'm not a cat-nya.",
            "I'm the light cruiser, Tama.  ¶ I'm ¶  not a cat-nya.",
            "I'm the light cruiser, Tama. I'm  ¶ not ¶  a cat-nya.",
            "I'm the light cruiser, Tama. I'm not  ¶ a ¶  cat-nya.",
            "I'm the light cruiser, Tama. I'm not a  ¶ cat ¶ -nya.",
            "I'm the light cruiser, Tama. I'm not a cat- ¶ nya ¶ .",
        ];

        string[] expectedJapaneseQuestions = [
            " ¶ 軽巡 ¶ 、多摩です。猫じゃないにゃ。",
            "軽巡、 ¶ 多摩 ¶ です。猫じゃないにゃ。",
            "軽巡、多摩 ¶ です ¶ 。猫じゃないにゃ。",
            "軽巡、多摩です。 ¶ 猫 ¶ じゃないにゃ。",
            "軽巡、多摩です。猫 ¶ じゃない ¶ にゃ。",
            "軽巡、多摩です。猫じゃない ¶ にゃ ¶ 。",
        ];

        pipeline.Setup(x => x.Pipe(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await aligner.AlignSymmetric(englishText, japaneseText);

        pipeline.Verify(x => x.Pipe(
            It.Is<string[]>(q => q.SequenceEqual(expectedEnglishQuestions)),
            japaneseText,
            It.IsAny<CancellationToken>()), Times.Once);

        pipeline.Verify(x => x.Pipe(
            It.Is<string[]>(q => q.SequenceEqual(expectedJapaneseQuestions)),
            englishText,
            It.IsAny<CancellationToken>()), Times.Once);

        pipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FiltersLowScoreAndCombinesReverseAlignment()
    {
        string englishText = "foo bar";
        string japaneseText = "ほげ　ぴよ";

        pipeline.Setup(x => x.Pipe(It.IsAny<string[]>(), japaneseText, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new QuestionAnsweringPrediction(Score: 0, Start: 4, End: 7, Answer: "ぴよ"), // low score
                new QuestionAnsweringPrediction(Score: 1, Start: 4, End: 7, Answer: "ぴよ"), // bar (4,7) -> ぴよ (3,5)
            ]);

        pipeline.Setup(x => x.Pipe(It.IsAny<string[]>(), englishText, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new QuestionAnsweringPrediction(Score: 1, Start: 0, End: 3, Answer: "foo"), // ほげ (0,2) -> foo (0,3)
                new QuestionAnsweringPrediction(Score: 0, Start: 0, End: 3, Answer: "foo"), // low score
            ]);

        IEnumerable<Alignment> result = await aligner.AlignSymmetric(englishText, japaneseText);

        Assert.Equal([
            new Alignment(0, 3, 0, 2), // ほげ (0,2) <- foo (0,3)
            new Alignment(4, 7, 3, 5), // bar (4,7) -> ぴよ (3,5)
        ], result);
    }

    [Fact]
    public async Task SnapsPredictionsToTokens()
    {
        string englishText = "It's alright";
        string japaneseText = "大丈夫";

        pipeline.Setup(x => x.Pipe(It.IsAny<string[]>(), japaneseText, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new QuestionAnsweringPrediction(Score: 0, Start: 0, End: 0, Answer: ""),
                new QuestionAnsweringPrediction(Score: 0, Start: 0, End: 0, Answer: ""),
            ]);

        pipeline.Setup(x => x.Pipe(It.IsAny<string[]>(), englishText, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new QuestionAnsweringPrediction(Score: 1, Start: 2, End: 10, Answer: "'s alrig"),
            ]);

        IEnumerable<Alignment> result = await aligner.AlignSymmetric(englishText, japaneseText);

        Assert.Equal([new Alignment(0, 12, 0, 3)], result);
    }

    // Test cases generated by running a random selection of Zuihou quotes through align.py, with and without
    // --no-simplify. test-quotes.tsv contains quotes exported from Kibana and converted to TSV (two columns:
    // english.text and japanese.text):
    //
    // $ while IFS=$'\t' read -r en ja; do ./align.py --from-language en --to-language ja --from-text "$en" --to-text "$ja" --symmetric --symmetric-mode OR; done <test-quotes.tsv | tee test-quotes_aligned_simplified.txt
    // $ while IFS=$'\t' read -r en ja; do ./align.py --from-language en --to-language ja --from-text "$en" --to-text "$ja" --symmetric --symmetric-mode OR --no-simplify; done <test-quotes.tsv | tee test-quotes_aligned_raw.txt
    // $ paste test-quotes_aligned_raw.txt test-quotes_aligned_simplified.txt test-quotes.tsv | tr -d $'\r' | head -n10 | \
    //   awk -F $'\t' '{ print "    [InlineData(\n        \"" $1 "\",\n        \"" $2 "\",\n        \"" $3 "\",\n        \"" $4 "\"\n    )]" }' | tee test-quotes_inlinedata.txt
    //
    // Note that there are many possible, valid simplifications of the same input, and the algorithm used may not even
    // necessarily produce the shortest possible result (see comments in simplify.py and simplify_slow.py). This only
    // tests that our implementation matches the Python code; correctness needs to be confirmed using the visualizer.
    //
    // See https://github.com/maxkagamine/word-alignment-demo
    [Theory]
    [InlineData(
        "1,3,9,11,8,15,0,1,8,15,1,2,15,16,0,1,15,16,1,2,16,21,2,3,22,27,3,4,22,27,4,6,28,35,3,4,28,35,4,6,35,36,6,7,37,43,7,9,43,44,11,12,47,50,26,27,47,50,27,28,51,61,13,15,51,61,15,16,62,69,24,26,70,72,21,22,70,72,22,23,70,72,23,24,75,79,16,18,79,80,16,18,80,85,16,18,86,91,18,20,86,91,20,21,91,92,28,29,93,97,29,30,93,97,30,31,100,109,31,33,100,109,33,35,110,116,33,35,116,117,35,36,118,122,35,36,125,128,48,49,125,128,49,51,125,128,51,52,129,136,36,38,129,136,38,39,129,136,39,40,137,146,46,48,147,149,43,44,147,149,44,45,147,149,45,46,152,157,40,41,152,157,41,43,158,165,40,41,158,165,41,43,165,166,52,53,169,172,56,57,169,172,57,60,169,172,60,61,169,172,61,64,173,177,54,56,173,177,56,57,173,177,57,60,173,177,60,61,173,177,61,64,180,185,54,56,180,185,56,57,180,185,57,60,180,185,60,61,180,185,61,64,186,190,56,57,186,190,57,60,186,190,60,61,186,190,61,64,191,194,56,57,191,194,57,60,191,194,60,61,191,194,61,64,197,203,75,77,197,203,77,78,197,203,78,80,197,203,80,81,204,206,73,75,211,215,69,71,211,215,71,72,216,220,72,73,228,232,65,67,233,238,67,69,238,239,81,82,8,15,0,1,15,16,0,1,16,21,0,1,8,15,1,2,15,16,1,2,16,21,1,2,8,15,2,3,15,16,2,3,16,21,2,3,22,27,3,4,28,35,4,6,35,36,6,7,37,43,7,9,1,3,9,11,43,44,11,12,51,61,13,15,51,61,15,16,75,79,16,18,79,80,16,18,80,85,16,18,86,91,18,20,86,91,20,21,70,72,21,22,70,72,22,23,70,72,23,24,62,69,24,26,47,50,26,27,47,50,27,28,91,92,28,29,93,97,29,30,93,97,30,31,100,109,31,33,110,116,33,35,116,117,35,36,129,136,36,38,129,136,38,39,129,136,39,40,152,157,40,41,158,165,41,43,147,149,43,44,147,149,44,45,147,149,45,46,137,146,46,48,125,128,48,49,125,128,49,51,125,128,51,52,165,166,52,53,180,185,54,56,180,185,56,57,186,190,57,60,169,172,60,61,173,177,60,61,169,172,61,64,173,177,61,64,228,232,65,67,233,238,65,67,228,232,67,69,233,238,67,69,211,215,69,71,211,215,71,72,216,220,72,73,204,206,73,75,197,203,75,77,197,203,77,78,197,203,78,80,197,203,80,81,238,239,81,82",
        "1,3,9,11,8,21,0,3,22,35,3,6,35,36,6,7,37,43,7,9,43,44,11,12,47,50,26,28,51,61,13,16,62,69,24,26,70,72,21,24,75,85,16,18,86,91,18,21,91,92,28,29,93,97,29,31,100,109,31,35,110,116,33,35,116,122,35,36,125,128,48,52,129,136,36,40,137,146,46,48,147,149,43,46,152,165,40,43,165,166,52,53,169,172,56,64,173,177,54,64,180,185,54,64,186,194,56,64,197,203,75,81,204,206,73,75,211,215,69,72,216,220,72,73,228,238,65,69,238,239,81,82",
        "I'm the Shouhou-class light carrier, Zuihou. I was originally planned as a high-speed oiler, then a submarine tender, then I was finally completed as a light carrier. I may have a small body but I fought to the last days of the Task Force!",
        "祥鳳型軽空母、瑞鳳です。 元々は高速給油艦として計画され、次に潜水母艦、最終的に軽空母として完成しました。 小柄なボディだけれど、機動部隊最後の日まで敢闘しました！"
    )]
    [InlineData(
        "4,10,0,2,11,13,2,3,17,20,3,4,17,20,4,6,20,21,9,10,22,24,10,12,24,25,12,13,26,33,13,15,33,34,15,16,35,38,22,24,35,38,24,25,35,38,25,27,35,38,27,29,43,49,22,24,43,49,24,25,43,49,25,27,43,49,27,29,50,54,22,24,50,54,24,25,50,54,25,27,50,54,27,29,55,62,19,21,55,62,21,22,66,72,16,18,66,72,18,19,72,73,29,30,74,78,30,32,74,78,32,33,74,78,33,34,74,78,34,36,74,78,36,37,78,81,30,32,78,81,32,33,78,81,33,34,78,81,34,36,78,81,36,37,89,94,38,40,95,96,38,40,97,103,38,40,103,104,40,41,4,10,0,2,11,13,2,3,11,13,3,4,11,13,4,6,13,16,6,7,13,16,7,9,20,21,9,10,22,24,10,12,24,25,12,13,26,33,13,15,33,34,15,16,66,72,16,18,66,72,18,19,55,62,19,21,50,54,22,24,43,49,24,25,35,38,25,27,39,42,25,27,43,49,25,27,35,38,27,29,39,42,27,29,43,49,27,29,72,73,29,30,74,78,30,32,82,85,32,33,85,88,32,33,82,85,33,34,85,88,33,34,74,78,34,36,78,81,34,36,74,78,36,37,78,81,36,37,89,94,38,40,95,96,38,40,97,103,38,40,103,104,40,41",
        "4,10,0,2,11,13,2,6,13,16,6,9,17,20,3,6,20,21,9,10,22,24,10,12,24,25,12,13,26,33,13,15,33,34,15,16,35,38,22,29,39,42,25,29,43,54,22,29,55,62,19,22,66,72,16,19,72,73,29,30,74,81,30,37,82,88,32,34,89,103,38,40,103,104,40,41",
        "The Tenzan is... huh? Uh, Admiral? Can you please stop groping my hangar? Yeah... you're being a bother.",
        "天山は…って、あれ？うん、提督？格納庫弄るの止めてくれない？うん…ていうか、邪魔。"
    )]
    [InlineData(
        "0,4,0,3,4,7,0,3,10,13,4,6,10,13,6,7,10,13,7,10,10,13,10,11,14,18,4,6,14,18,6,7,14,18,7,10,14,18,10,11,18,19,11,12,20,23,16,17,20,23,17,18,20,23,18,19,20,23,19,21,20,23,21,22,20,23,22,23,24,25,16,17,24,25,17,18,24,25,18,19,24,25,19,21,24,25,21,22,24,25,22,23,26,30,16,17,26,30,17,18,26,30,18,19,26,30,19,21,26,30,21,22,26,30,22,23,33,36,16,17,33,36,17,18,33,36,18,19,33,36,19,21,33,36,21,22,33,36,22,23,42,46,14,16,47,51,14,16,51,52,25,26,0,4,0,3,4,7,0,3,0,4,3,4,4,7,3,4,10,13,4,6,14,18,4,6,10,13,6,7,10,13,7,10,10,13,10,11,14,18,10,11,18,19,11,12,33,36,12,14,42,46,14,16,47,51,14,16,26,30,16,17,26,30,17,18,31,32,17,18,33,36,17,18,37,39,17,18,40,41,17,18,42,46,17,18,47,51,17,18,20,23,19,21,20,23,21,22,20,23,22,23,51,52,25,26",
        "0,7,0,4,10,18,4,11,18,19,11,12,20,30,16,23,31,51,17,18,33,36,12,14,33,36,16,17,33,36,18,23,42,51,14,16,51,52,25,26",
        "Yeah... I got beat. Can I take a bit of a long bath?",
        "ううん…やられちゃった。少し長湯してもいいかな、ね？"
    )]
    [InlineData(
        "8,11,2,3,8,11,3,4,8,11,4,5,12,14,2,3,12,14,3,4,12,14,4,5,14,15,5,6,16,18,27,28,16,18,28,30,18,21,5,6,22,25,6,7,22,25,7,8,22,25,8,9,22,25,9,10,33,36,16,17,33,36,17,19,33,36,19,20,33,36,20,21,33,36,21,23,33,36,23,25,33,36,25,26,40,44,16,17,40,44,17,19,40,44,19,20,40,44,20,21,40,44,21,23,40,44,23,25,40,44,25,26,45,49,15,16,50,56,11,15,56,57,30,31,8,11,0,2,12,14,0,2,8,11,2,3,12,14,2,3,8,11,3,4,12,14,3,4,8,11,4,5,12,14,4,5,14,15,5,6,16,18,6,7,18,21,6,7,16,18,7,8,18,21,7,8,22,25,8,9,22,25,9,10,50,56,11,15,45,49,15,16,40,44,16,17,40,44,17,19,40,44,19,20,31,33,21,23,33,36,21,23,31,33,23,25,33,36,23,25,37,39,25,26,8,11,27,28,12,14,27,28,56,57,30,31",
        "8,14,0,5,8,14,27,28,14,15,5,6,16,18,6,8,16,18,27,30,18,21,5,8,22,25,6,10,31,33,21,25,33,36,16,26,37,44,25,26,40,44,16,25,45,49,15,16,50,56,11,15,56,57,30,31",
        "They... got me. Ah... But this won't go like Cape Engano.",
        "やら…れた。あ…でも、エンガノ岬のようにはいかないん、だから。"
    )]
    [InlineData(
        "0,5,0,2,0,5,2,3,0,5,3,4,0,5,4,5,6,10,0,2,6,10,2,3,6,10,3,4,6,10,4,5,10,11,5,6,6,10,0,2,6,10,2,3,0,5,3,4,6,10,3,4,0,5,4,5,6,10,4,5,10,11,5,6",
        "0,10,0,5,10,11,5,6",
        "Looks good.",
        "いいかもね。"
    )]
    [InlineData(
        "1,3,2,4,4,10,0,2,10,11,4,5,13,16,10,11,13,16,11,14,13,16,14,16,13,16,16,17,17,23,10,11,17,23,11,14,17,23,14,16,17,23,16,17,27,32,5,6,27,32,6,8,33,40,8,10,40,41,17,18,43,46,27,30,43,46,30,32,47,49,27,30,47,49,30,32,53,57,18,22,53,57,22,23,57,58,23,24,59,63,24,27,64,68,24,27,68,69,32,33,4,10,0,2,0,1,2,4,1,3,2,4,10,11,4,5,27,32,5,6,27,32,6,8,33,40,8,10,17,23,10,11,17,23,11,14,17,23,14,16,17,23,16,17,40,41,17,18,59,63,24,27,64,68,24,27,42,43,27,30,43,46,27,30,47,49,27,30,42,43,30,32,43,46,30,32,47,49,30,32,68,69,32,33",
        "0,3,2,4,4,10,0,2,10,11,4,5,13,23,10,17,27,32,5,8,33,40,8,10,40,41,17,18,42,49,27,32,53,57,18,23,57,58,23,24,59,68,24,27,68,69,32,33",
        "I'm Zuihou! I've gotten my major remodel! I'll do my very, very best!",
        "瑞鳳です！大規模改装しちゃえました！ぎゅーっと、もっと頑張ります！"
    )]
    [InlineData(
        "3,8,0,2,12,16,3,5,17,22,3,5,17,22,5,7,17,22,7,8,23,26,3,5,23,26,5,7,23,26,7,8,26,27,8,9,30,32,9,12,30,32,12,15,30,32,15,17,30,32,17,18,30,32,18,19,33,37,9,12,33,37,12,15,33,37,15,17,33,37,17,18,33,37,18,19,38,42,9,12,38,42,12,15,38,42,15,17,38,42,17,18,38,42,18,19,45,47,9,12,45,47,12,15,45,47,15,17,45,47,17,18,45,47,18,19,47,50,9,12,47,50,12,15,47,50,15,17,47,50,17,18,47,50,18,19,51,54,9,12,51,54,12,15,51,54,15,17,51,54,17,18,51,54,18,19,55,58,9,12,55,58,12,15,55,58,15,17,55,58,17,18,55,58,18,19,58,59,19,20,3,8,0,2,9,11,2,3,12,16,3,5,17,22,5,7,23,26,5,7,17,22,7,8,23,26,7,8,26,27,8,9,51,54,9,12,55,58,9,12,45,47,12,15,47,50,12,15,33,37,15,17,38,42,17,18,58,59,19,20",
        "3,8,0,2,9,11,2,3,12,26,3,5,17,26,5,8,26,27,8,9,30,42,9,19,45,58,9,19,58,59,19,20",
        "My armor is thin after all. It's best that I don't get hit.",
        "装甲は薄いからね。当たらなきゃいいのよ。"
    )]
    [InlineData(
        "0,2,0,1,0,2,1,2,2,5,0,1,2,5,1,2,2,5,2,3,2,5,3,6,5,11,0,1,5,11,1,2,5,11,2,3,5,11,3,6,11,12,6,7,15,21,18,20,15,21,20,22,15,21,22,23,15,21,23,24,15,21,24,25,22,24,18,20,22,24,20,22,22,24,22,23,22,24,23,24,22,24,24,25,25,29,7,11,25,29,11,13,30,40,13,15,41,43,18,20,41,43,20,22,41,43,22,23,41,43,23,24,41,43,24,25,44,46,16,17,44,46,17,18,44,46,18,20,51,54,16,17,51,54,17,18,51,54,18,20,54,55,25,26,5,11,0,1,5,11,1,2,0,2,2,3,2,5,2,3,5,11,2,3,5,11,3,6,11,12,6,7,5,11,7,11,25,29,11,13,30,40,13,15,51,54,16,17,51,54,17,18,51,54,18,20,41,43,20,22,22,24,23,24,22,24,24,25,54,55,25,26",
        "0,2,0,3,2,11,0,6,5,11,7,11,11,12,6,7,15,24,18,25,25,29,7,13,30,40,13,15,41,43,18,25,44,46,16,20,51,54,16,20,54,55,25,26",
        "Ow...owowow. I wonder if this camouflage is of any use.",
        "痛た…たたた。あんまりこの迷彩は役に立たないのかな。"
    )]
    [InlineData(
        "1,3,13,15,1,3,15,16,4,8,13,15,4,8,15,16,9,13,9,10,9,13,10,11,9,13,11,12,17,23,3,5,24,29,3,5,24,29,5,6,30,34,9,10,30,34,10,11,30,34,11,12,43,53,7,9,54,58,24,27,54,58,27,28,58,59,16,17,62,65,30,33,62,65,33,34,62,65,34,35,66,70,28,30,79,83,28,30,84,88,24,27,84,88,27,28,89,91,24,27,89,91,27,28,93,95,20,21,93,95,21,23,98,103,17,18,104,111,18,20,111,112,35,36,17,23,3,5,24,29,5,6,43,53,7,9,30,34,9,10,35,39,9,10,30,34,10,11,35,39,10,11,9,13,11,12,4,8,13,15,4,8,15,16,58,59,16,17,98,103,17,18,104,111,18,20,93,95,20,21,93,95,21,23,84,88,24,27,89,91,24,27,84,88,27,28,89,91,27,28,66,70,28,30,71,72,28,30,73,78,28,30,79,83,28,30,62,65,30,33,111,112,35,36",
        "1,8,13,16,9,13,9,12,17,29,3,5,24,29,5,6,30,34,9,12,35,39,9,11,43,53,7,9,54,58,24,28,58,59,16,17,62,65,30,35,66,83,28,30,84,91,24,28,93,95,20,23,98,103,17,18,104,111,18,20,111,112,35,36",
        "I'm glad that my attack corps were able to contribute lots. I can play a large role even if I'm a light carrier.",
        "瑞鳳の航空隊が活躍したの、やった。軽空母だって、頑張れば活躍できるのよ。"
    )]
    [InlineData(
        "2,5,7,9,2,5,9,11,6,12,7,9,6,12,9,11,18,20,0,3,18,20,3,6,21,25,0,3,21,25,3,6,26,31,0,3,26,31,3,6,31,32,11,12,18,20,0,3,21,25,0,3,26,31,0,3,18,20,3,6,21,25,3,6,26,31,3,6,6,12,7,9,2,5,9,11,31,32,11,12",
        "2,12,7,11,18,31,0,6,31,32,11,12",
        "We'll decide this at long range.",
        "アウトレンジ、決めます。"
    )]
    public void SimplifiesAlignments(string inputStr, string expectedStr, string fromText, string toText)
    {
        Alignment[] input = StringToAlignments(inputStr);
        Alignment[] expected = StringToAlignments(expectedStr);

        var actual = WordAligner.SimplifyAlignments(input, fromText, toText);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// align.py outputs alignments as a list of integers, where every group of four elements corresponds to the
    /// parameters of our <see cref="Alignment"/> struct.
    /// </summary>
    private static Alignment[] StringToAlignments(string str) =>
        str.Split(',').Select(ushort.Parse).Chunk(4).Select(x => new Alignment(x[0], x[1], x[2], x[3])).ToArray();

    public void Dispose() => aligner.Dispose();
}
