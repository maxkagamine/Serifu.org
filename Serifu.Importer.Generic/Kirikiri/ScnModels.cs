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

using Kagamine.Extensions.Collections;

namespace Serifu.Importer.Generic.Kirikiri;

internal sealed record ScnFile(
    ValueArray<ScnScene> Scenes
);

internal sealed record ScnScene(
    string Label,
    ValueArray<ScnSceneText> Texts,
    ScnTranslations<string> Title
);

internal sealed record ScnSceneText(
    string? SpeakerName,
    string? SpeakerDisplayName,
    ScnTranslations<ScnSceneTextTranslation> Translations,
    ValueArray<ScnVoiceFile> VoiceFiles
);

internal sealed record ScnTranslations<T>(
    T English,
    T Japanese
);

internal sealed record ScnSceneTextTranslation(
    string TranslatedSpeakerName,
    string FormattedText
);

internal sealed record ScnVoiceFile(
    string Name,
    int Pan,
    int Type,
    string Voice
);
