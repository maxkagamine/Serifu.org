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

using System.Collections;

namespace Serifu.Data.Elasticsearch;

public sealed class SearchResults : IReadOnlyList<SearchResult>
{
    private readonly IReadOnlyList<SearchResult> results;

    internal SearchResults(SearchLanguage searchLanguage, IReadOnlyList<SearchResult> results)
    {
        this.results = results;

        SearchLanguage = searchLanguage;
    }

    public SearchLanguage SearchLanguage { get; }

    public SearchResult this[int index] => results[index];
    public int Count => results.Count;
    public IEnumerator<SearchResult> GetEnumerator() => results.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)results).GetEnumerator();
}

public sealed class SearchResult
{
    public required Quote Quote { get; init; }

    public required IReadOnlyList<Range> EnglishHighlights { get; init; }

    public required IReadOnlyList<Range> JapaneseHighlights { get; init; }
}
