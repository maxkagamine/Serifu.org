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
using System.Diagnostics.CodeAnalysis;

namespace Serifu.Web.Models;

public class TranslationViewModel
{
    public TranslationViewModel(Translation translation, string language, string audioFileBaseUrl)
    {
        Language = language;
        SpeakerName = translation.SpeakerName;
        Text = translation.Text;
        Notes = translation.Notes;
        AudioFileUrl = translation.AudioFile is null ? null : $"{audioFileBaseUrl}/{translation.AudioFile}";
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
    public bool HasSpeakerName => !string.IsNullOrEmpty(SpeakerName);

    /// <inheritdoc cref="Translation.Text"/>
    public string Text { get; }

    /// <inheritdoc cref="Translation.Notes"/>
    public string Notes { get; }

    /// <summary>
    /// Whether there are notes for this translation.
    /// </summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    /// <summary>
    /// The audio file URL, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public string? AudioFileUrl { get; }

    /// <summary>
    /// Whether the translation has audio.
    /// </summary>
    [MemberNotNullWhen(true, nameof(AudioFileUrl))]
    public bool HasAudioFile => AudioFileUrl is not null;
}
