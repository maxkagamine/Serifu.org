using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Serifu.Importer.Skyrim.Resolvers;

using Mutagen.Bethesda.FormKeys.SkyrimSE;

internal class ConditionsResolver
{
    private readonly FactionResolver factionResolver;
    private readonly UniqueVoiceTypeResolver uniqueVoiceTypeResolver;
    private readonly Lazy<QuestAliasResolver> questAliasResolver;
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ISpeakerFactory speakerFactory;
    private readonly IFormIdProvider formIdProvider;
    private readonly ILogger logger;

    /// <summary>
    /// Represents either a negated condition or one that matches a very broad set of NPCs (non-unique GetIsVoiceType,
    /// GetIsRace, etc.). These conditions can only be used to narrow down a set of speakers.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    private class FilterCondition(Predicate<Speaker> predicate, string conditionString)
    {
        public bool Matches(Speaker speaker) => predicate(speaker);

        public override string ToString() => conditionString; // For debugging/logging
    }

    /// <summary>
    /// Represents a non-negated condition that matches a specific NPC or group of NPCs (GetIsID, GetInFaction, etc.).
    /// These conditions can both narrow down a set of speakers and produce the initial set to be narrowed down.
    /// </summary>
    private class ProducerCondition : FilterCondition
    {
        public ProducerCondition(SpeakersResult speakers, string conditionString)
            : this(speakers, speakers.Select(s => s.FormKey).ToHashSet(), conditionString)
        { }

        private ProducerCondition(SpeakersResult speakers, HashSet<FormKey> formKeys, string conditionString)
            : base(s => formKeys.Contains(s.FormKey), conditionString)
        {
            Speakers = speakers;
        }

        /// <summary>
        /// The speakers included in this condition. Also carries the faction editor ID for GetInFaction conditions.
        /// </summary>
        public SpeakersResult Speakers { get; }
    }

    public ConditionsResolver(
        FactionResolver factionResolver,
        UniqueVoiceTypeResolver uniqueVoiceTypeResolver,
        Lazy<QuestAliasResolver> questAliasResolver,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        IFormIdProvider formIdProvider,
        ILogger logger)
    {
        this.factionResolver = factionResolver;
        this.uniqueVoiceTypeResolver = uniqueVoiceTypeResolver;
        this.questAliasResolver = questAliasResolver;
        this.env = env;
        this.speakerFactory = speakerFactory;
        this.formIdProvider = formIdProvider;
        this.logger = logger.ForContext<ConditionsResolver>();
    }

    /// <inheritdoc cref="Resolve(SpeakersResult, IQuestGetter?, IReadOnlyList{IConditionGetter}[], ImmutableHashSet{ValueTuple{FormKey, int}})"/>
    /// <param name="conditions">The conditions to evaluate.</param>
    public SpeakersResult Resolve(
        IQuestGetter? quest,
        IReadOnlyList<IConditionGetter> conditions,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
        => Resolve(SpeakersResult.Empty, quest, [conditions], processedQuestAliases);

    /// <inheritdoc cref="Resolve(SpeakersResult, IQuestGetter?, IReadOnlyList{IConditionGetter}[], ImmutableHashSet{ValueTuple{FormKey, int}})"/>
    public SpeakersResult Resolve(
        SpeakersResult initialCollection,
        IQuestGetter? quest,
        params IReadOnlyList<IConditionGetter>[] conditionSets)
        => Resolve(initialCollection, quest, conditionSets, []);

    /// <summary>
    /// Evaluates the conditions and returns the matching speakers.
    /// </summary>
    /// <param name="initialCollection">
    /// A set of speakers to filter based on the conditions. Ignored if empty, and the conditions will be evaluated as
    /// though starting with all NPCs (though only if the conditions include a "producer" such as GetIsID or
    /// GetInFaction will anything be returned).
    /// </param>
    /// <param name="quest">
    /// The quest associated with the conditions. Used for GetIsAliasRef.
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
        IQuestGetter? quest,
        IReadOnlyList<IConditionGetter>[] conditionSets,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
    {
        // Build conditions
        IEnumerable<IEnumerable<IConditionGetter>> groupedConditions = GroupOrConditions(conditionSets);
        FilterCondition?[][] mappedFilterProducers = groupedConditions
            .Select(g => g.Select(c => CreateCondition(c, quest, processedQuestAliases)).ToArray()).ToArray();

        if (initialCollection.IsEmpty)
        {
            // Use one of the "producers" to produce the initial collection
            FilterCondition?[]? groupToUseAsProducer =
                mappedFilterProducers.FirstOrDefault(g => g.All(c => c is ProducerCondition)) ??
                mappedFilterProducers.FirstOrDefault(g => g.Any(c => c is ProducerCondition));

            if (groupToUseAsProducer is null)
            {
                // There are no producers in the condition sets, and we don't have any NPCs to filter
                return SpeakersResult.Empty;
            }

            initialCollection = SpeakersResult.Combine(groupToUseAsProducer
                .OfType<ProducerCondition>().Select(c => c.Speakers));
        }

        FilterCondition[] combinedOrGroups = mappedFilterProducers
            .Where(g => !g.Any(c => c is null))
            .Select(g => g.Length == 1 ? g[0]! : new FilterCondition(
                s => g.Any(c => c!.Matches(s)),
                $"({string.Join(" OR ", g.Select(c => c!.ToString()))})"))
            .ToArray();

        logger.Debug("Evaluating conditions: {ConditionString}",
            string.Join(" AND ", combinedOrGroups.Select(c => c.ToString())));

        // Evaluate conditions
        IEnumerable<Speaker> speakers = combinedOrGroups.Aggregate(initialCollection,
            (IEnumerable<Speaker> speakers, FilterCondition filter) => speakers.Where(filter.Matches));

        HashSet<string> factions = mappedFilterProducers
            .SelectMany(g => g.OfType<ProducerCondition>().SelectMany(c => c.Speakers.Factions))
            .Concat(initialCollection.Factions)
            .ToHashSet();

        return new SpeakersResult(speakers, factions);
    }

    /// <summary>
    /// Creates a <see cref="FilterCondition"/> or <see cref="ProducerCondition"/> that can execute the given condition,
    /// or returns <see langword="null"/> if the condition should be ignored (does not apply to the speaker or depends
    /// on game state).
    /// </summary>
    private FilterCondition? CreateCondition(
        IConditionGetter condition,
        IQuestGetter? quest,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
    {
        if (condition is not IConditionFloatGetter { Data.RunOnType: Condition.RunOnType.Subject } conditionFloat)
        {
            return null;
        }

        bool isNegated = IsNegated(conditionFloat);

        FilterCondition? result = conditionFloat.Data switch
        {
            IGetInFactionConditionDataGetter getInFaction => CreateGetInFactionCondition(getInFaction, isNegated),
            IGetIsAliasRefConditionDataGetter getIsAliasRef => CreateGetIsAliasRefCondition(getIsAliasRef, quest, processedQuestAliases),
            IGetIsIDConditionDataGetter getIsID => CreateGetIsIDCondition(getIsID),
            IGetIsRaceConditionDataGetter getIsRace => CreateGetIsRaceCondition(getIsRace),
            IGetIsSexConditionDataGetter getIsSex => CreateGetIsSexCondition(getIsSex),
            IGetIsVoiceTypeConditionDataGetter getIsVoiceType => CreateGetIsVoiceTypeCondition(getIsVoiceType),
            IIsChildConditionDataGetter => CreateIsChildCondition(),
            IIsGuardConditionDataGetter => CreateIsGuardCondition(),
            IIsInListConditionDataGetter isInList => CreateIsInListCondition(isInList),
            _ => null
        };

        if (isNegated && result is not null)
        {
            FilterCondition inner = result;
            result = new FilterCondition(s => !inner.Matches(s), $"NOT {inner}");
        }

        return result;
    }

    private ProducerCondition? CreateGetInFactionCondition(IGetInFactionConditionDataGetter data, bool isNegated)
    {
        if (data.Faction.Link.TryResolve(env, out IFactionGetter? faction) &&
            factionResolver.Resolve(faction, includeFactionOverrides: !isNegated) is { IsEmpty: false } result)
        {
            // If an NPC's faction rank is -1, they're not in the faction initially but can be at some point in the
            // game; these NPCs should always match for both the non-negated and negated check. The reverse is a bit
            // more difficult: for example, Jarl Ballin's GovRuling rank is 0 (true), but if the player makes poor
            // choices he might be exiled and removed from the faction. We can tell when an NPC might be added to a
            // faction, but there's no way to definitively know that an NPC could be removed from a faction.
            if (isNegated)
            {
                result = new SpeakersResult(result.Where(s => speakerFactory.GetNpcProperty(s, npc =>
                    npc.Factions.First(f => f.Faction.FormKey == faction.FormKey).Rank != -1)), result.Factions);
            }

            return new ProducerCondition(result, $"GetInFaction({formIdProvider.GetFormattedString(faction)})");
        }

        return null;
    }

    private ProducerCondition? CreateGetIsAliasRefCondition(
        IGetIsAliasRefConditionDataGetter data,
        IQuestGetter? quest,
        ImmutableHashSet<(FormKey, int)> processedQuestAliases)
    {
        // Have confirmed that "ReferenceAliasIndex" here refers to the alias ID (not its index in the list)
        if (quest is not null &&
            questAliasResolver.Value.Resolve(quest, data.ReferenceAliasIndex, processedQuestAliases)
                is { IsEmpty: false } result)
        {
            string? aliasName = quest.Aliases.First(a => a.ID == data.ReferenceAliasIndex).Name;
            return new ProducerCondition(result, $"GetIsAliasRef({data.ReferenceAliasIndex:D3} {aliasName})");
        }

        return null;
    }

    private ProducerCondition? CreateGetIsIDCondition(IGetIsIDConditionDataGetter data)
    {
        if (data.Object.Link.Cast<INpcGetter>().TryResolve(env, out INpcGetter? npc))
        {
            return new ProducerCondition(speakerFactory.Create(npc), $"GetIsID({formIdProvider.GetFormattedString(npc)})");
        }

        if (data.Object.Link.Cast<ITalkingActivatorGetter>().TryResolve(env, out ITalkingActivatorGetter? tact))
        {
            return new ProducerCondition(speakerFactory.Create(tact), $"GetIsID({formIdProvider.GetFormattedString(tact)})");
        }

        return null;
    }

    private FilterCondition? CreateGetIsRaceCondition(IGetIsRaceConditionDataGetter data)
    {
        if (data.Race.Link.TryResolve(env, out IRaceGetter? race))
        {
            return new FilterCondition(
                s => speakerFactory.GetNpcProperty(s, npc => npc.Race.FormKey) == race.FormKey,
                $"GetIsRace({formIdProvider.GetFormattedString(race)})");
        }

        return null;
    }

    private FilterCondition CreateGetIsSexCondition(IGetIsSexConditionDataGetter data)
    {
        return new FilterCondition(
            s => (speakerFactory.GetNpcProperty(s, npc => npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female)) ?
                    MaleFemaleGender.Female : MaleFemaleGender.Male) == data.MaleFemaleGender,
            $"GetIsSex({data.MaleFemaleGender})");
    }

    private FilterCondition? CreateGetIsVoiceTypeCondition(IGetIsVoiceTypeConditionDataGetter data)
    {
        if (data.VoiceTypeOrList.Link.TryResolve(env, out IVoiceTypeOrListGetter? voiceTypeOrList))
        {
            IEnumerable<IVoiceTypeGetter> voiceTypes = voiceTypeOrList switch
            {
                IVoiceTypeGetter voiceType => [voiceType],
                IFormListGetter list => list.Items
                    .Select(x => x.Cast<IVoiceTypeGetter>().Resolve(env))
                    .OfType<IVoiceTypeGetter>(),
                _ => throw new UnreachableException("VoiceTypeOrList resolved to neither a voice type nor a list.")
            };

            string conditionString = $"GetIsVoiceType({formIdProvider.GetFormattedString(voiceTypeOrList)})";

            // Check if all of the voice types are unique
            SpeakersResult[] uniqueVoiceTypeResults = voiceTypes.Select(uniqueVoiceTypeResolver.Resolve).ToArray();
            if (uniqueVoiceTypeResults.All(r => !r.IsEmpty))
            {
                return new ProducerCondition(
                    new(uniqueVoiceTypeResults.SelectMany(s => s)),
                    conditionString);
            }

            HashSet<string> voiceTypeEditorIds = voiceTypes.Select(v => v.EditorID).OfType<string>().ToHashSet();

            return new FilterCondition(
                s => s.HasVoiceType && voiceTypeEditorIds.Contains(s.VoiceType),
                conditionString);
        }

        return null;
    }

    private FilterCondition CreateIsChildCondition()
    {
        return new FilterCondition(
            s => speakerFactory.GetNpcProperty(s, npc => npc.Race.Resolve(env)?.Flags.HasFlag(Race.Flag.Child)) == true,
            "IsChild()");
    }

    private FilterCondition CreateIsGuardCondition()
    {
        return new FilterCondition(
            s => speakerFactory.GetNpcProperty(s, npc => npc.Factions.FirstOrDefault(f =>
                    f.Faction.FormKey == Skyrim.Faction.IsGuardFaction.FormKey)) is not null,
            "IsGuard()");
    }

    private FilterCondition? CreateIsInListCondition(IIsInListConditionDataGetter data)
    {
        if (data.FormList.Link.TryResolve(env, out IFormListGetter? formList))
        {
            return new FilterCondition(
                s => formList.Items.Any(x => x.FormKey == s.FormKey),
                $"IsInList({formIdProvider.GetFormattedString(formList)})");
        }

        return null;
    }

    /// <summary>
    /// Groups consecutive OR conditions, such that the elements in the top enumerable are AND'd together, and the
    /// elements in each inner enumerable are OR'd.
    /// </summary>
    private static IEnumerable<IEnumerable<IConditionGetter>> GroupOrConditions(
        IEnumerable<IReadOnlyList<IConditionGetter>> conditionSets)
    {
        List<IConditionGetter> orGroup = [];

        foreach (IReadOnlyList<IConditionGetter> conditions in conditionSets)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                orGroup.Add(conditions[i]);

                if (i == conditions.Count - 1 || !conditions[i].Flags.HasFlag(Condition.Flag.OR))
                {
                    yield return orGroup.ToArray();
                    orGroup.Clear();
                }
            }
        }
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
