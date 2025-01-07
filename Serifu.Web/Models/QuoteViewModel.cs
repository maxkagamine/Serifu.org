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
using Serifu.Data.Elasticsearch;

namespace Serifu.Web.Models;

public class QuoteViewModel
{
    public QuoteViewModel(SearchResult result, bool englishFirst, string audioFileBaseUrl)
    {
        var quote = result.Quote;

        Id = quote.Id;
        Source = quote.Source;
        EnglishSpeakerName = quote.English.SpeakerName;

        TranslationViewModel english = new(this, quote.English, result.EnglishHighlights, "en", audioFileBaseUrl);
        TranslationViewModel japanese = new(this, quote.Japanese, result.JapaneseHighlights, "ja", audioFileBaseUrl);

        (Left, Right) = englishFirst ? (english, japanese) : (japanese, english);
    }

    /// <inheritdoc cref="Quote.Id"/>
    public long Id { get; }

    /// <inheritdoc cref="Quote.Source"/>
    public Source Source { get; }

    /// <summary>
    /// The translation appearing on the left, corresponding to the search language.
    /// </summary>
    public TranslationViewModel Left { get; }

    /// <summary>
    /// The translation appearing on the right, opposite of the search language.
    /// </summary>
    public TranslationViewModel Right { get; }

    /// <summary>
    /// Speaker name from the English translation. Used for Kancolle Wiki links.
    /// </summary>
    public string EnglishSpeakerName { get; }
}
