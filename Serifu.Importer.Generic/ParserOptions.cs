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

namespace Serifu.Importer.Generic;

internal abstract class ParserOptions
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
}
