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

using Microsoft.AspNetCore.Html;
using Serifu.Data;
using Serifu.Web.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Serifu.Web.Models;

public class TranslationViewModel
{
    public TranslationViewModel(
        QuoteViewModel quoteViewModel,
        Translation translation,
        IReadOnlyList<Range> highlights,
        string language,
        AudioFileUrlProvider audioFileUrlProvider)
    {
        Language = language;
        SpeakerName = translation.SpeakerName;
        Context = translation.Context;
        Text = Highlighter.ApplyHighlights(translation.Text, highlights);
        Notes = string.IsNullOrWhiteSpace(translation.Notes) ? null : new HtmlString(translation.Notes);
        AudioFileUrl = audioFileUrlProvider.GetUrl(translation.AudioFile);
        Quote = quoteViewModel;
    }

    /// <summary>
    /// Two-letter ISO language code (either "en" or "ja").
    /// </summary>
    public string Language { get; }

    /// <inheritdoc cref="Translation.SpeakerName"/>
    public string SpeakerName { get; }

    /// <summary>
    /// Whether the quote has a speaker name; <see langword="false"/> for generic lines or if the speaker is unknown.
    /// </summary>
    public bool HasSpeakerName => !string.IsNullOrWhiteSpace(SpeakerName);

    /// <inheritdoc cref="Translation.Context"/>
    public string Context { get; }

    /// <summary>
    /// Whether this quote has a context.
    /// </summary>
    public bool HasContext => !string.IsNullOrWhiteSpace(Context);

    /// <summary>
    /// The translated quote, HTML-encoded with highlights.
    /// </summary>
    public IHtmlContent Text { get; }

    /// <summary>
    /// Translation notes, if any.
    /// </summary>
    public IHtmlContent? Notes { get; }

    /// <summary>
    /// Whether there are notes for this translation.
    /// </summary>
    public bool HasNotes => Notes is not null;

    /// <summary>
    /// The audio file URL, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public string? AudioFileUrl { get; }

    /// <summary>
    /// Whether the translation has audio.
    /// </summary>
    [MemberNotNullWhen(true, nameof(AudioFileUrl))]
    public bool HasAudioFile => AudioFileUrl is not null;

    public QuoteViewModel Quote { get; }
}
