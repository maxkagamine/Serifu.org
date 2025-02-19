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

using System.Collections;
using System.Diagnostics;

namespace Serifu.Importer.Skyrim;

[DebuggerDisplay("Count = {Speakers.Count}")]
public class SpeakersResult : IEnumerable<Speaker>
{
    public static readonly SpeakersResult Empty = new([]);

    public SpeakersResult(IEnumerable<Speaker> speakers, IEnumerable<string>? factions = null)
    {
        Speakers = speakers.Distinct().ToArray();
        Factions = factions?.ToHashSet() ?? [];
    }

    public static implicit operator SpeakersResult(Speaker? speaker) => speaker is null ? Empty : new([speaker]);

    public static SpeakersResult Combine(IEnumerable<SpeakersResult> results) =>
        new(results.SelectMany(s => s.Speakers), results.SelectMany(s => s.Factions));

    /// <summary>
    /// The resolved speakers.
    /// </summary>
    public IReadOnlyCollection<Speaker> Speakers { get; }

    /// <summary>
    /// Whether <see cref="Speakers"/> contains any elements.
    /// </summary>
    public bool IsEmpty => Speakers.Count == 0;

    /// <summary>
    /// Factions associated with the dialogue (via non-negated GetInFaction conditions), by editor ID. This is used to
    /// prioritize certain names or NPCs for a faction once all conditions have been evaluated and it comes time to
    /// select from the set of eligible NPCs.
    /// </summary>
    public IReadOnlySet<string> Factions { get; }

    public IEnumerator<Speaker> GetEnumerator() => Speakers.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Speakers).GetEnumerator();
}
