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

namespace Serifu.Importer.Generic.Kirikiri;

internal sealed class ScnParserOptions : ParserOptions
{
    /// <summary>
    /// Index of Japanese text in the <c>.scenes[].texts[][2]</c> array.
    /// </summary>
    public int JapaneseLanguageIndex { get; set; } = 0;

    /// <summary>
    /// Index of English text in the <c>.scenes[].texts[][2]</c> array.
    /// </summary>
    public int EnglishLanguageIndex { get; set; } = 1;

    /// <summary>
    /// Whether to use <c>.scenes[].title</c> as the <see cref="ParsedQuoteTranslation.Context"/>.
    /// </summary>
    public bool UseSceneTitleAsContext { get; set; } = true;

    /// <summary>
    /// Replacement scene titles, per-language, where the key is the title as written in <c>.scenes[].title</c> for the
    /// given language and the value is the string to use instead.
    /// </summary>
    public Dictionary<Language, Dictionary<string, string>> SceneTitleMap { get; set; } = [];

    /// <summary>
    /// Scenes to skip, as an array of "filename*label" (basename without scn/json extension, e.g. "02_00_b.ks*start").
    /// </summary>
    public HashSet<string> ExcludedScenes { get; set; } = [];
}
