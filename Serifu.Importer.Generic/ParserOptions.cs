using Serifu.Data;
using System.ComponentModel.DataAnnotations;

namespace Serifu.Importer.Generic;

internal abstract class ParserOptions : IValidatableObject
{
    /// <summary>
    /// The name of the <see cref="IParser"/> class to use for parsing the <see cref="DialogueFiles"/>.
    /// </summary>
    public string Parser { get; set; } = "";

    /// <summary>
    /// The game being imported. This is set automatically to the name of the config section so that it can be passed to
    /// the parser to handle certain differences between games.
    /// </summary>
    public Source Source { get; set; }

    /// <summary>
    /// The directory containing the extracted/converted game files.
    /// </summary>
    public string BaseDirectory { get; set; } = "";

    /// <summary>
    /// The dialogue files to parse, as a list of glob patterns per-language, relative to <see cref="BaseDirectory"/>.
    /// Use <see cref="Language.Multilingual"/> if the translations are contained within a single file.
    /// </summary>
    public Dictionary<Language, List<string>> DialogueFiles { get; set; } = [];

    /// <summary>
    /// The path to either language's audio file directory, relative to <see cref="BaseDirectory"/>.
    /// </summary>
    public Dictionary<Language, string> AudioDirectories { get; set; } = [];

    /// <summary>
    /// Replacement speaker names, per-language.
    /// </summary>
    public Dictionary<Language, Dictionary<string, string>> SpeakerNameMap { get; set; } = [];

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Source == default)
        {
            // Kancolle doesn't use the generic importer, so this means the source was not set properly
            yield return new ValidationResult("Source should have been set automatically.", [nameof(Source)]);
        }

        if (string.IsNullOrEmpty(BaseDirectory))
        {
            yield return new ValidationResult($"Base directory for {Source} should be set in user secrets.", [nameof(BaseDirectory)]);
        }
        else if (!Directory.Exists(BaseDirectory))
        {
            yield return new ValidationResult($"Base directory \"{BaseDirectory}\" does not exist.", [nameof(BaseDirectory)]);
        }

        if (DialogueFiles.Count == 0)
        {
            yield return new ValidationResult("Dialogue files is empty.", [nameof(DialogueFiles)]);
        }
        else
        {
            foreach (var (language, globs) in DialogueFiles)
            {
                if (globs.Count == 0)
                {
                    yield return new ValidationResult($"Dialogue files array for {language} is empty.", [nameof(DialogueFiles)]);
                }
            }
        }

        if (AudioDirectories.Count == 0)
        {
            yield return new ValidationResult("Audio directories is empty.", [nameof(AudioDirectories)]);
        }
    }
}
