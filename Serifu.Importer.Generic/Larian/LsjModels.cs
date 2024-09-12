namespace Serifu.Importer.Generic.Larian;

internal record LsjFile(LsjSave Save)
{ }

internal record LsjSave(LsjRegions Regions);

internal record LsjRegions(LsjTemplates? Templates, LsjOrigins? Origins, LsjVoiceMetaData? VoiceMetaData);

internal record LsjTemplates(LsjGameObject[] GameObjects);

internal record LsjOrigins(LsjOrigin[] Origin);

internal record LsjVoiceMetaData(LsjVoiceSpeakerMetaData[] VoiceSpeakerMetaData);

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

/// <summary>
/// Represents a collection of dialogue text to audio file mappings for a given speaker.
/// </summary>
/// <param name="MapKey">The speaker ID or "NARRATOR".</param>
/// <param name="MapValue">The speaker's voice lines.</param>
internal record LsjVoiceSpeakerMetaData(
    LsjString MapKey,
    LsjVoiceSpeakerMetaDataValue[] MapValue);

internal record LsjVoiceSpeakerMetaDataValue(LsjVoiceTextMetaData[]? VoiceTextMetaData);

/// <summary>
/// Represents a mapping of dialogue text to audio file.
/// </summary>
/// <param name="MapKey">The localization XML string ID.</param>
/// <param name="MapValue">An object containing the audio filename (.wem).</param>
internal record LsjVoiceTextMetaData(
    LsjString MapKey,
    LsjVoiceTextMetaDataValue[] MapValue);

internal record LsjVoiceTextMetaDataValue(LsjString Source);

internal record struct LsjString(string Value);

internal record struct LsjGuid(Guid Value);

internal record struct LsjTranslatedString(string Handle);
