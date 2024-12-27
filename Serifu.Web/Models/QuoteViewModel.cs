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
using System.Globalization;

namespace Serifu.Web.Models;

public class QuoteViewModel
{
    public QuoteViewModel(Quote quote, bool englishFirst, string audioFileBaseUrl)
    {
        Id = quote.Id;
        Source = quote.Source;

        TranslationViewModel english = new(quote.English, "en", audioFileBaseUrl);
        TranslationViewModel japanese = new(quote.Japanese, "ja", audioFileBaseUrl);

        (Left, Right) = englishFirst ? (english, japanese) : (japanese, english);

        Context = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en" ?
            quote.English.Context : quote.Japanese.Context;
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

    /// <inheritdoc cref="Translation.Context"/>
    public string Context { get; }

    /// <summary>
    /// Whether this quote has a context.
    /// </summary>
    public bool HasContext => !string.IsNullOrWhiteSpace(Context);
}
