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

using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Immutable;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class QuestAliasResolver
{
    private readonly ConditionsResolver conditionsResolver;
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ISpeakerFactory speakerFactory;
    private readonly ILogger logger;

    public QuestAliasResolver(
        ConditionsResolver conditionsResolver,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        ILogger logger)
    {
        this.conditionsResolver = conditionsResolver;
        this.env = env;
        this.speakerFactory = speakerFactory;
        this.logger = logger.ForContext<QuestAliasResolver>();
    }

    public SpeakersResult Resolve(IQuestGetter quest, int aliasId)
    {
        SpeakersResult result = Resolve(quest, aliasId, []);

        if (result.IsEmpty)
        {
            logger.Debug("No NPC found for {@Quest} alias {QuestAlias}.", quest, aliasId);
        }

        return result;
    }

    /// <summary>
    /// Attempts to find the NPC(s) (or TACT) that could fill a quest alias by various means.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <param name="aliasId">The alias ID (ALST, not the array index).</param>
    /// <param name="processedQuestAliases">
    /// The quest form keys and alias IDs of any quest aliases traversed in the current call stack, to avoid recursion.
    /// </param>
    /// <returns>The resolved speakers, or empty if the quest alias does not point to any specific NPC.</returns>
    public SpeakersResult Resolve(IQuestGetter quest, int aliasId, ImmutableHashSet<(FormKey, int)> processedQuestAliases)
    {
        if (processedQuestAliases.Contains((quest.FormKey, aliasId)))
        {
            logger.Warning("Detected cyclic reference at {@Quest} alias {QuestAlias}.", quest, aliasId);
            return SpeakersResult.Empty;
        }

        processedQuestAliases = processedQuestAliases.Add((quest.FormKey, aliasId));

        if (quest.Aliases.SingleOrDefault(a => a.ID == aliasId) is not IQuestAliasGetter alias)
        {
            logger.Warning("Alias {QuestAlias} not found in {@Quest}.", aliasId, quest);
            return SpeakersResult.Empty;
        }

        if (alias.CreateReferenceToObject is not null &&
            alias.CreateReferenceToObject.Object.TryResolve(env, out ISkyrimMajorRecordGetter? aliasCreatedObject))
        {
            const string CreatedObjectIsNotNpcOrTact = "{@Quest} alias {QuestAlias} has Create Reference to Object but it is not an NPC or TACT.";
            const string CreatedObjectFound = "Found {@NpcOrTact} in {@Quest} alias {QuestAlias}'s Create Reference to Object.";

            if (aliasCreatedObject is INpcGetter aliasCreatedNpc)
            {
                logger.Debug(CreatedObjectFound, aliasCreatedNpc, quest, aliasId);
                return FoundNpc(quest, alias, aliasCreatedNpc);
            }
            else if (aliasCreatedObject is ITalkingActivatorGetter aliasCreatedTact)
            {
                logger.Debug(CreatedObjectFound, aliasCreatedTact, quest, aliasId);
                return FoundTact(aliasCreatedTact);
            }
            else
            {
                logger.Debug(CreatedObjectIsNotNpcOrTact, quest, aliasId);
            }
        }

        if (alias.ForcedReference.TryResolve(env, out IPlacedGetter? forcedReference))
        {
            const string ReferenceIsNotNpcOrTact = "{@Quest} alias {QuestAlias} has a Forced Reference but it is not an NPC or TACT.";
            const string ReferenceLacksBase = "{@Quest} alias {QuestAlias} has a Forced Reference to {@Reference} but it lacks a Base.";
            const string ReferenceFound = "Found {@NpcOrTact} in {@Quest} alias {QuestAlias}'s Forced Reference.";

            if (forcedReference is IPlacedNpcGetter achr)
            {
                if (!achr.Base.TryResolve(env, out INpcGetter? achrBase))
                {
                    logger.Debug(ReferenceLacksBase, quest, aliasId, achr);
                }
                else
                {
                    logger.Debug(ReferenceFound, achrBase, quest, aliasId);
                    return FoundNpc(quest, alias, achrBase);
                }
            }
            else if (forcedReference is IPlacedObjectGetter refr)
            {
                if (!refr.Base.TryResolve(env, out IPlaceableObjectGetter? refrBase))
                {
                    logger.Debug(ReferenceLacksBase, quest, aliasId, refr);
                }
                else if (refrBase is not ITalkingActivatorGetter refrBaseTact)
                {
                    logger.Debug(ReferenceIsNotNpcOrTact, quest, aliasId);
                }
                else
                {
                    logger.Debug(ReferenceFound, refrBaseTact, quest, aliasId);
                    return FoundTact(refrBaseTact);
                }
            }
            else
            {
                logger.Debug(ReferenceIsNotNpcOrTact, quest, aliasId);
            }
        }

        if (alias.UniqueActor.TryResolve(env, out INpcGetter? uniqueActor))
        {
            logger.Debug("Found {@Npc} in {@Quest} alias {QuestAlias}'s Unique Actor.",
                uniqueActor, quest, aliasId);

            return FoundNpc(quest, alias, uniqueActor);
        }

        if (alias.Conditions.Count > 0)
        {
            SpeakersResult conditionsResult = conditionsResolver.Resolve(alias.Conditions, processedQuestAliases);

            if (conditionsResult.IsEmpty)
            {
                logger.Debug("{@Quest} alias {QuestAlias} has Conditions but we couldn't find any specific NPCs.",
                    quest, aliasId);
            }
            else
            {
                logger.Debug("Found {@Npcs} in {@Quest} alias {QuestAlias}'s Conditions.",
                    conditionsResult, quest, aliasId);

                return conditionsResult;
            }
        }

        if (alias.External is not null &&
            alias.External.Quest.TryResolve(env, out IQuestGetter? externalQuest) &&
            alias.External.AliasID is int externalAliasId)
        {
            logger.Debug("Following {@Quest} alias {QuestAlias}'s External Alias Reference to {@ExternalQuest} alias {ExternalAliasId}.",
                quest, aliasId, externalQuest, externalAliasId);

            return Resolve(externalQuest, externalAliasId, processedQuestAliases);
        }

        return SpeakersResult.Empty;
    }

    private Speaker FoundNpc(IQuestGetter quest, IQuestAliasGetter alias, INpcGetter npc)
    {
        Speaker speaker = speakerFactory.Create(npc);

        if (alias.DisplayName.TryResolve(env, out IMessageGetter? displayName) && displayName.Name is not null)
        {
            var (english, japanese) = displayName.Name;

            logger.Debug("{@Quest} alias {QuestAlias} replaces {@Npc}'s name with {DisplayName}.",
                quest, alias.ID, npc, english);

            return speaker with
            {
                EnglishName = english,
                JapaneseName = japanese
            };
        }

        return speaker;
    }

    private Speaker FoundTact(ITalkingActivatorGetter tact) => speakerFactory.Create(tact);
}
