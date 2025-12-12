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

namespace Serifu.Importer.Generic.Larian;

internal sealed record LsjFile(LsjSave Save)
{ }

internal sealed record LsjSave(LsjRegions Regions);

internal sealed record LsjRegions(LsjTemplates? Templates, LsjOrigins? Origins, LsjVoiceMetaData? VoiceMetaData);

internal sealed record LsjTemplates(LsjGameObject[] GameObjects);

internal sealed record LsjOrigins(LsjOrigin[] Origin);

internal sealed record LsjVoiceMetaData(LsjVoiceSpeakerMetaData[] VoiceSpeakerMetaData);

/// <summary>
/// Represents either a game object (character, item, and other things like lights that we don't care about) or a
/// template for a game object.
/// </summary>
/// <param name="DisplayName">The localization XML string ID.</param>
/// <param name="MapKey">For our purposes, the "speaker ID" and primary key for this type.</param>
/// <param name="Name">The object or template's internal name.</param>
/// <param name="TemplateName">The object's template MapKey.</param>
/// <param name="ParentTemplateId">The template's template MapKey.</param>
internal sealed record LsjGameObject(
    LsjTranslatedString? DisplayName,
    LsjGuid? MapKey,
    LsjString? Name,
    LsjGuid? TemplateName,
    LsjGuid? ParentTemplateId);

/// <summary>
/// Represents one of the origin characters.
/// </summary>
/// <param name="DisplayName">The localization XML string ID.</param>
/// <param name="GlobalTemplate">For our purposes, the "speaker ID" and primary key for this type.</param>
internal sealed record LsjOrigin(
    LsjTranslatedString? DisplayName,
    LsjGuid? GlobalTemplate);

/// <summary>
/// Represents a collection of dialogue text to audio file mappings for a given speaker.
/// </summary>
/// <param name="MapKey">The speaker ID or "NARRATOR".</param>
/// <param name="MapValue">The speaker's voice lines.</param>
internal sealed record LsjVoiceSpeakerMetaData(
    LsjString MapKey,
    LsjVoiceSpeakerMetaDataValue[] MapValue);

internal sealed record LsjVoiceSpeakerMetaDataValue(LsjVoiceTextMetaData[]? VoiceTextMetaData);

/// <summary>
/// Represents a mapping of dialogue text to audio file.
/// </summary>
/// <param name="MapKey">The localization XML string ID.</param>
/// <param name="MapValue">An object containing the audio filename (.wem).</param>
internal sealed record LsjVoiceTextMetaData(
    LsjString MapKey,
    LsjVoiceTextMetaDataValue[] MapValue);

internal sealed record LsjVoiceTextMetaDataValue(LsjString Source);

internal record struct LsjString(string Value);

internal record struct LsjGuid(Guid Value);

internal record struct LsjTranslatedString(string Handle);
