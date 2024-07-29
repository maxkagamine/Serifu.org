using System.Collections;

namespace Serifu.Importer.Skyrim;

public class SpeakersResult : IEnumerable<Speaker>
{
    public static readonly SpeakersResult Empty = new([]);

    public SpeakersResult(IEnumerable<Speaker> speakers)
    {
        Speakers = speakers.ToArray();
    }

    public static implicit operator SpeakersResult(Speaker? speaker) => speaker is null ? Empty : new([speaker]);

    public static SpeakersResult Combine(IEnumerable<SpeakersResult> results) =>
        new(results.SelectMany(s => s.Speakers).Distinct())
        {
            Factions = results.SelectMany(s => s.Factions).ToHashSet()
        };

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
    public IReadOnlySet<string> Factions { get; init; } = new HashSet<string>(); // TODO: Use ReadOnlySet<string>.Empty in .NET 9

    public IEnumerator<Speaker> GetEnumerator() => Speakers.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Speakers).GetEnumerator();
}
