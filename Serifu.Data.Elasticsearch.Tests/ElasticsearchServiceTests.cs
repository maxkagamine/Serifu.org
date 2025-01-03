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
using Moq;
using Serilog;

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
}
