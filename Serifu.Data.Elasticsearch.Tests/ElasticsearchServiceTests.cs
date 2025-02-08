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

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;
using FluentAssertions;
using Kagamine.Extensions.Collections;
using Moq;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using Range = System.Range;

namespace Serifu.Data.Elasticsearch.Tests;

public class ElasticsearchServiceTests
{
    private readonly Mock<ElasticsearchClient> client;
    private readonly ElasticsearchService service;

    public ElasticsearchServiceTests()
    {
        client = new Mock<ElasticsearchClient>(MockBehavior.Strict);
        service = new ElasticsearchService(client.Object, new LoggerConfiguration().CreateLogger());
    }

    [Fact]
    public void ExtractHighlights()
    {
        const char H = ElasticsearchService.HighlightMarker;
        string original = "2nd ship of the Agano-class light cruisers, Noshiro. Reporting for duty. Pleased to meet you!";
        string highlighted = $"2nd ship of the {H}Agano{H}-{H}class{H} light cruisers, Noshiro. {H}Reporting{H} {H}for{H} {H}duty{H}. Pleased to meet you!";
        string[] expectedHighlights = ["Agano-class", "Reporting for duty"];

        var ranges = ElasticsearchService.ExtractHighlights(highlighted);

        ranges.Select(r => original[r]).Should().Equal(expectedHighlights);
    }

    [Fact]
    public void MapHighlightsToTargetLanguage()
    {
        // 阿賀野型軽巡二番艦、能代。着任しました。よろしくどうぞ！
        // \_____/\_/\____/ \__/ \_________/ \__________/
        //    1    2    3    4        5           6
        //
        //  ___3__          _1_   _1_   _____2______    __4__    _______5________    ________6________
        // /      \        /   \ /   \ /            \  /     \  /                \  /                 \
        // 2nd ship of the Agano-class light cruisers, Noshiro. Reporting for duty. Pleased to meet you!

        string english = "2nd ship of the Agano-class light cruisers, Noshiro. Reporting for duty. Pleased to meet you!";
        string japanese = "阿賀野型軽巡二番艦、能代。着任しました。よろしくどうぞ！";
        var alignments = DecodeAlignmentData("AAAIAAYACQAQABUAAAAEABYAGwAAAAQAHAAqAAQABgAsADMACgAMADUARwANABMASQBcABQAGwA=");

        Range[] japaneseHighlights = [new(2, 8), new(13, 15), new(17, 19)];
        Range[] expectedEnglishHighlights = [new(0, 8), new(16, 42), new(53, 71)];

        // The first Japanese highlight should map to four alignments which combine to two English highlights (bridging
        // both spaces and a hyphen), while the second and third both match the same alignment, producing one highlight.
        japaneseHighlights.Select(r => japanese[r]).Should().Equal([
            "野型軽巡二番", "着任", "した"]);
        expectedEnglishHighlights.Select(r => english[r]).Should().Equal([
            "2nd ship", "Agano-class light cruisers", "Reporting for duty"]);

        var actualEnglishHighlights = ElasticsearchService.MapHighlightsToTargetLanguage(
            japaneseHighlights, SearchLanguage.Japanese, alignments, english);

        actualEnglishHighlights.Should().Equal(expectedEnglishHighlights);
    }

    [Theory]
    [InlineData(
        "能代", new[] { "japanese.text", "japanese.text.conjugations" },
        "a query in Japanese should search the Japanese translation")]
    [InlineData(
        "light　cruiser", new[] { "english.text", "english.text.conjugations" },
        "a query in English should search the English translation, even if it contains full-width spaces")]
    [InlineData(
        "𪚲", new[] { "japanese.text.kanji" },
        "a query containing a single kanji should search the dedicated kanji subfield (since the regular field is restricted to bigrams)")]
        // ↑ This is also an example of a four-byte kanji (which, since .NET uses UTF-16, means the string has a length of 2)
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Simpler for tests")]
    public async Task QueriesCorrectFields(string query, string[] expectedFields, string because)
    {
        var unexpectedFields = new[]
        {
            "english.text",
            "english.text.conjugations",
            "japanese.text",
            "japanese.text.conjugations",
            "japanese.text.kanji",
        }.Except(expectedFields);

        client.Setup(x => x.SearchAsync<Quote>(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .Throws<NotImplementedException>();

        try
        {
            await service.Search(query, CancellationToken.None);
        }
        catch (NotImplementedException) { }

        // There's no way to actually inspect the query without reflection or serializing
        var request = (SearchRequest)client.Invocations.Single().Arguments[0];
        var json = client.Object.RequestResponseSerializer.SerializeToString(request.Query);
        json.Should().ContainAll(expectedFields.Select(f => $"\"{f}\""), because)
            .And.NotContainAny(unexpectedFields.Select(f => $"\"{f}\""), because);
    }

    [Fact]
    public async Task ReturnsExpectedSearchResults()
    {
        Quote[] quotes = [
            new()
            {
                Id = 1069446856704,
                Source = Source.Kancolle,
                English = new()
                {
                    SpeakerName = "Pola",
                    Context = "Introduction",
                    Text = "Good morning~. I'm the 3rd ship of the Zara-class heavy cruisers, Pola~. I dare any venture. I'll do my best.",
                    WordCount = 22,
                    Notes = "The ship's motto was \"Ardisco ad Ogni Impresa\" or \"I dare any venture\"."
                },
                Japanese = new Translation()
                {
                    SpeakerName = "Pola",
                    Context = "入手/ログイン",
                    Text = "Buon Giorno～。ザラ級重巡の三番艦～、ポーラです～。何にでも挑戦したいお年頃。頑張ります～。",
                    WordCount = 16,
                    AudioFile = "e6/c3/f44719cc2562d4d0fe77eeaa4643eaf98a23.mp3"
                },
                AlignmentData = DecodeAlignmentData("AAAMAAAACwAXAB8AEwAWACAAIgASABMAJwArAA0AEAAsADEADQAQADIAQAAQABIAQgBGABgAHQBLAE8AIwApAFAAUwAfACMAVABbACMAKQBdAGwALAAxAGIAZAAjACkAaABsACMAKQA="),
                DateImported = DateTime.Parse("2024-12-28T22:30:47.6133659")
            },
            new()
            {
                Id = 1065151889420,
                Source = Source.Kancolle,
                English = new()
                {
                    SpeakerName = "Zara",
                    Context = "Joining the Fleet",
                    Text = "Zara-class heavy cruiser, Zara! Setting sail! Fleet, ahead full!",
                    WordCount = 10
                },
                Japanese = new()
                {
                    SpeakerName = "Zara",
                    Context = "編成",
                    Text = "ザラ級重巡、ザラ！ 抜錨します！ 艦隊前に、行きます！",
                    WordCount = 9,
                    AudioFile = "ad/f0/e29d56d4a16d33f194e9576e958c3bbc18f9.mp3"
                },
                AlignmentData = DecodeAlignmentData("AAAEAAAAAwAFAAoAAAADAAsAGAADAAUAGgAeAAYACAAgACwACgAPAC4AMwARABMANQA/ABMAFQA1AD8AFgAaAA=="),
                DateImported = DateTime.Parse("2024-12-28T22:30:43.4581241")
            }
        ];

        const char H = ElasticsearchService.HighlightMarker;

        client.Setup(x => x.SearchAsync<Quote>(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponse<Quote>()
            {
                HitsMetadata = new()
                {
                    Hits = [
                        new()
                        {
                            Source = quotes[0],
                            Highlight = new Dictionary<string, IReadOnlyCollection<string>>()
                            {
                                // "ザラ級" maps to "Zara" and "class"; "重巡" to "heavy cruisers"; and "年頃" to nothing.
                                ["japanese.text"] = [$"Buon Giorno～。{H}ザラ{H}級{H}重巡{H}の三番艦～、ポーラです～。何にでも挑戦したいお{H}年頃{H}。頑張ります～。"]
                            }
                        },
                        new()
                        {
                            // Testing without highlights
                            Source = quotes[1],
                        }
                    ]
                }
            });

        SearchResults results = await service.Search("重巡", CancellationToken.None);

        results.Should().HaveCount(2);
        results.SearchLanguage.Should().Be(SearchLanguage.Japanese);

        results[0].Quote.Should().Be(quotes[0]);
        results[0].JapaneseHighlights.Should().Equal([new Range(13, 15), new Range(16, 18), new Range(41, 43)],
            "ザラ, 重巡, and 年頃 should be highlighted");
        results[0].EnglishHighlights.Should().Equal([new Range(39, 64)], """
            ザラ should hit the ザラ級 -> {Zara, class} alignment, and 重巡 should align to "heavy cruisers", all of which
            should combine into one highlight as they're adjacent
            """);

        results[1].Quote.Should().Be(quotes[1]);
        results[1].JapaneseHighlights.Should().BeEmpty();
        results[1].EnglishHighlights.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", ElasticsearchValidationError.TooShort)]
    [InlineData("a", ElasticsearchValidationError.TooShort)]
    [InlineData("a\u0301", ElasticsearchValidationError.TooShort)] // Accent combining character
    [InlineData("aa", null)]
    [InlineData("か", ElasticsearchValidationError.TooShort)]
    [InlineData("か\u3099", ElasticsearchValidationError.TooShort)] // Dakuten combining character
    [InlineData("かか", null)]
    [InlineData("鏡", null)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", ElasticsearchValidationError.TooLong)]
    [InlineData("あああああああああああああああああああああああああああああああああ", ElasticsearchValidationError.TooLong)]
    public async Task ThrowsIfQueryIsTooShortOrLong(string query, ElasticsearchValidationError? error)
    {
        Func<Task> func = () => service.Search(query, CancellationToken.None);

        if (error is null)
        {
            await func.Should().ThrowAsync<MockException>(); // Attempted the search
        }
        else
        {
            (await func.Should().ThrowAsync<ElasticsearchValidationException>())
                .Which.Error.Should().Be(error);
        }
    }

    private static ValueArray<Alignment> DecodeAlignmentData(string base64) =>
        ValueArray.FromBytes<Alignment>(Convert.FromBase64String(base64));
}
