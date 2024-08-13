using Kagamine.Extensions.Collections;

namespace Serifu.Importer.Generic.Kirikiri;

internal record ScnFile(
    ValueArray<ScnScene> Scenes
);

internal record ScnScene(
    string Label,
    ValueArray<ScnSceneText> Texts,
    ScnTranslations<string> Title
);

internal record ScnSceneText(
    string? SpeakerName,
    string? SpeakerDisplayName,
    ScnTranslations<ScnSceneTextTranslation> Translations,
    ValueArray<ScnVoiceFile> VoiceFiles
);

internal record ScnTranslations<T>(
    T English,
    T Japanese
);

internal record ScnSceneTextTranslation(
    string TranslatedSpeakerName,
    string FormattedText
    //string? FuriganaPlainText,
    //string? PlainText
);

internal record ScnVoiceFile(
    string Name,
    int Pan,
    int Type,
    string Voice
);
