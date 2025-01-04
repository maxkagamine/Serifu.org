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
using FluentAssertions;
using Kagamine.Extensions.Collections;
using Moq;
using Serilog;
using Range = System.Range;

namespace Serifu.Data.Elasticsearch.Tests;

public class ElasticsearchServiceTests
{
    private readonly Mock<ElasticsearchClient> client;
    private readonly ElasticsearchService service;

    public ElasticsearchServiceTests()
    {
        client = new Mock<ElasticsearchClient>();
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

    private ValueArray<Alignment> DecodeAlignmentData(string base64) =>
        ValueArray.FromBytes<Alignment>(Convert.FromBase64String(base64));
}
