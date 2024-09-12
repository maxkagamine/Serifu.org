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

using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Larian;

[JsonSerializable(typeof(LsjFile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class LsjSourceGenerationContext : JsonSerializerContext
{ }

internal record LsjFile(LsjSave Save)
{ }

internal record LsjSave(LsjRegions Regions);

internal record LsjRegions(LsjTemplates? Templates, LsjOrigins? Origins);

internal record LsjTemplates(LsjGameObject[] GameObjects);

internal record LsjOrigins(LsjOrigin[] Origin);

/// <summary>
/// Represents either a game object (character, item, and other things like lights that we don't care about) or a
/// template for a game object.
/// </summary>
/// <param name="DisplayName">The localization XML string ID.</param>
/// <param name="MapKey">For our purposes, the "speaker ID" and primary key for this type.</param>
/// <param name="Name">The object or template's internal name.</param>
/// <param name="TemplateName">The object's template MapKey.</param>
/// <param name="ParentTemplateId">The template's template MapKey.</param>
internal record LsjGameObject(
    LsjTranslatedStringValue? DisplayName,
    LsjGuidValue? MapKey,
    LsjStringValue? Name,
    LsjGuidValue? TemplateName,
    LsjGuidValue? ParentTemplateId);

/// <summary>
/// Represents one of the origin characters.
/// </summary>
/// <param name="DisplayName">The localization XML string ID.</param>
/// <param name="GlobalTemplate">For our purposes, the "speaker ID" and primary key for this type.</param>
internal record LsjOrigin(
    LsjTranslatedStringValue? DisplayName,
    LsjGuidValue? GlobalTemplate);

internal record LsjStringValue(string Value);

internal record LsjGuidValue(Guid Value);

internal record LsjTranslatedStringValue(string Handle);
