namespace Serifu.Importer.Generic.Larian;

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
internal record LsjOrigin(
    LsjTranslatedString? DisplayName,
    LsjGuid? GlobalTemplate);

internal record struct LsjString(string Value);

internal record struct LsjGuid(Guid Value);

internal record struct LsjTranslatedString(string Handle);
