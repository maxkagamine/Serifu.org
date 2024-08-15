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

namespace Serifu.Importer.Generic.CatSystem2;

internal class CstParserOptions : ParserOptions
{
    /// <summary>
    /// Dump each scene to a readable format for analysis.
    /// </summary>
    public bool DumpCst { get; set; }

    /// <summary>
    /// Lines with any of these audio files (sans extension) will be dropped. Used to remove incorrect translations
    /// as a result of censorship.
    /// </summary>
    public HashSet<string> ExcludedLinesByAudioFile { get; set; } = [];
}
