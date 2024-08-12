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

internal class KsParserOptions : ParserOptions
{
    // The config binder appends to arrays, and there doesn't seem to be a way to make it replace the default instead.
    public static readonly string[] DefaultLineSeparatorTags = ["l", "p"];

    /// <summary>
    /// These tags are used to separate lines of dialogue.
    /// </summary>
    public string[]? LineSeparatorTags { get; set; }

    /// <summary>
    /// If any of these tags are present in the dialogue line, the quote will stop there and the rest will be discarded.
    /// This is used to strip off ", said Tsubaki" etc., since that extra bit seems a little weird in the context where
    /// we'd be displaying the quotes.
    /// </summary>
    public string[] QuoteStopTags { get; set; } = [];
}
