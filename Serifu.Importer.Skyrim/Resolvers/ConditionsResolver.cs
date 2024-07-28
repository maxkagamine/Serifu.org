using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Immutable;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class ConditionsResolver
{
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ISpeakerFactory speakerFactory;
    private readonly ILogger logger;

    /// <summary>
    /// Represents either a negated condition or one that matches a very broad set of NPCs (non-unique GetIsVoiceType,
    /// GetIsRace, etc.). These conditions can only be used to narrow down a set of speakers.
    /// </summary>
    private class FilterCondition(Predicate<Speaker> predicate)
    {
        public bool Matches(Speaker speaker) => predicate(speaker);
    }

    /// <summary>
    /// Represents a non-negated condition that matches a specific NPC or group of NPCs (GetIsID, GetInFaction, etc.).
    /// These conditions can both narrow down a set of speakers and produce the initial set to be narrowed down.
    /// </summary>
    private class ProducerCondition : FilterCondition
    {
        public ProducerCondition(IEnumerable<Speaker> speakers)
            : this(speakers, speakers.Select(s => s.FormKey).ToHashSet())
        { }

        private ProducerCondition(IEnumerable<Speaker> speakers, HashSet<FormKey> formKeys)
            : base(s => formKeys.Contains(s.FormKey))
        {
            Speakers = speakers;
        }

        public IEnumerable<Speaker> Speakers { get; }
    }

    public ConditionsResolver(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        ILogger logger)
    {
        this.env = env;
        this.speakerFactory = speakerFactory;
        this.logger = logger.ForContext<ConditionsResolver>();
    }

    /// <inheritdoc cref="Resolve(SpeakersResult, IReadOnlyList{IConditionGetter}[], ImmutableHashSet{ValueTuple{FormKey, int}})"/>
    /// <param name="conditions">The conditions to evaluate.</param>
    public SpeakersResult Resolve(
        IReadOnlyList<IConditionGetter> conditions,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
        => Resolve(SpeakersResult.Empty, [conditions], processedQuestAliases);

    /// <inheritdoc cref="Resolve(SpeakersResult, IReadOnlyList{IConditionGetter}[], ImmutableHashSet{ValueTuple{FormKey, int}})"/>
    public SpeakersResult Resolve(
        SpeakersResult initialCollection,
        params IReadOnlyList<IConditionGetter>[] conditionSets)
        => Resolve(initialCollection, conditionSets, []);

    /// <summary>
    /// Evaluates the conditions and returns the matching speakers.
    /// </summary>
    /// <param name="initialCollection">
    /// A set of speakers to filter based on the conditions. Ignored if empty, and the conditions will be evaluated as
    /// though starting with all NPCs (though only if the conditions include a "producer" such as GetIsID or
    /// GetInFaction will anything be returned).
    /// </param>
    /// <param name="conditionSets">
    /// One or more sets of conditions which are AND'd together, ignoring the OR flag on the last condition in a group
    /// if it happens to be set (the quest dialogue conditions and dialogue info conditions aren't simply concatenated
    /// for this reason).
    /// </param>
    /// <param name="processedQuestAliases">
    /// The quest form keys and alias IDs of any quest aliases traversed in the current call stack, to avoid recursion.
    /// </param>
    /// <returns>
    /// The speakers matching the conditions, along with the editor IDs of any factions referenced (and not negated).
    /// <para/>
    /// If the conditions only contain broad filters such as GetIsRace, an empty result set will be returned, rather
    /// than return every NPC in the game with that race etc. (to whom the dialogue applies is in that case most likely
    /// based on the game state, which is not something we can evaluate).
    /// </returns>
    public SpeakersResult Resolve(
        SpeakersResult initialCollection,
        IReadOnlyList<IConditionGetter>[] conditionSets,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
    {
        foreach (var condition in conditionSets.SelectMany(x => x))
        {
            if (condition is not IConditionFloatGetter { Data.RunOnType: Condition.RunOnType.Subject } conditionFloat)
            {
                continue;
            }

            // TODO
        }

        return SpeakersResult.Empty;
    }

    private static bool IsNegated(IConditionFloatGetter condition) => condition.CompareOperator switch
    {
        CompareOperator.EqualTo => condition.ComparisonValue != 1,
        CompareOperator.NotEqualTo => condition.ComparisonValue != 0,
        CompareOperator.GreaterThan => condition.ComparisonValue is >= 1 or < 0,
        CompareOperator.GreaterThanOrEqualTo => condition.ComparisonValue is > 1 or <= 0,
        _ => true
    };
}
