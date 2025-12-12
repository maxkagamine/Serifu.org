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

internal sealed class KsParserOptions : ParserOptions
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

    /// <summary>
    /// <para>
    ///     Whether to skip lines without a voice file. This helps to eliminate mismatched translations, as the TL team
    ///     is pretty much forced to keep voiced parts 1:1 with the Japanese, whereas it's not uncommon for the unvoiced
    ///     parts to be split into fewer/additional lines.
    /// </para>
    /// <para>
    ///     Key indexes will only consider the included voiced lines if true, so that if a label includes a different
    ///     number of unvoiced parts followed by a voiced line, the latter will be at index 0 even though the number of
    ///     lines preceeding it was different between the two languages.
    /// </para>
    /// <para>
    ///     This is sortof a lazy, nuclear solution to the problem, as it throws away a ton of perfectly good quotes
    ///     along with the bad ones. The ideal solution would be to go through every line that doesn't have an English
    ///     or Japanese translation and build a list of which lines on one side should be combined to match a line on
    ///     the other... but doing so would take many hours if not days.
    /// </para>
    /// </summary>
    public bool VoicedLinesOnly { get; set; }
}
