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

namespace Serifu.Importer.Generic;

internal interface IParser
{
    /// <summary>
    /// Parses a file and returns the dialogue lines as a collection of <see cref="ParsedQuoteTranslation"/>.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="language">Language hint.</param>
    IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language);
}

internal interface IParser<TOptions> : IParser where TOptions : ParserOptions;
