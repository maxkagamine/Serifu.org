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

    /// <summary>
    /// If there are multiple translations for a given key and language even after removing duplicates, by default an
    /// exception will be thrown. If this is true, the quote will be discarded instead and a warning logged.
    /// </summary>
    /// <remarks>
    /// Normally this would indicate a bug, but it can also happen if the audio file is used as a key and the dialogue
    /// appears in multiple scenes (different routes) but with slightly different text.
    /// </remarks>
    public bool IgnoreDuplicateKeysWithinLanguage { get; set; }

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
    }
}
