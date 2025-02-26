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

        if (alias.CreateReferenceToObject is not null)
        {
            const string CreatedObjectIsWrongType = "{@Quest} alias {QuestAlias} has a Create Reference to Object, but it's a {RecordType}.";
            const string CreatedObjectFound = "Found {@NpcOrTact} in {@Quest} alias {QuestAlias}'s Create Reference to Object.";

            if (alias.CreateReferenceToObject.Object.Cast<INpcGetter>().TryResolve(env, out var aliasCreatedNpc))
            {
                logger.Debug(CreatedObjectFound, aliasCreatedNpc, quest, aliasId);
                return FoundNpc(quest, alias, aliasCreatedNpc);
            }
            else if (alias.CreateReferenceToObject.Object.Cast<ITalkingActivatorGetter>().TryResolve(env, out var aliasCreatedTact))
            {
                logger.Debug(CreatedObjectFound, aliasCreatedTact, quest, aliasId);
                return FoundTact(quest, alias, aliasCreatedTact);
            }
            else
            {
                logger.Debug(CreatedObjectIsWrongType, quest, aliasId, alias.CreateReferenceToObject.Object.Resolve(env)?.GetRecordType());
            }
        }

        if (alias.ForcedReference.TryResolve(env, out IPlacedGetter? forcedReference))
        {
            const string ReferenceIsWrongType = "{@Quest} alias {QuestAlias} has a Forced Reference, but it's a {RecordType}.";
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
                    // Might be an XMarker (STAT), or in two cases an Activator (ACTI), but those won't have a voice
                    // type and they all seem to have a Speaker in their INFO anyway. The name might be different,
                    // though; in MS14 "Laid to Rest" [QUST:00025F3E] alias 5 has the display name "Child's Coffin," but
                    // since we fall back to the Speaker in [INFO:0003664F] we attribute the quote to "Helgi's Ghost,"
                    // which is technically correct just not what appears in game.
                    logger.Debug(ReferenceIsWrongType, quest, aliasId, refrBase.GetRecordType());
                }
                else
                {
                    logger.Debug(ReferenceFound, refrBaseTact, quest, aliasId);
                    return FoundTact(quest, alias, refrBaseTact);
                }
            }
            else
            {
                logger.Debug(ReferenceIsWrongType, quest, aliasId, forcedReference.GetRecordType());
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
            SpeakersResult conditionsResult = conditionsResolver.Resolve(quest, alias.Conditions, processedQuestAliases);

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

    private SpeakersResult FoundNpc(IQuestGetter quest, IQuestAliasGetter alias, INpcGetter npc)
        => WithDisplayName(quest, alias, speakerFactory.Create(npc));

    private Speaker FoundTact(IQuestGetter quest, IQuestAliasGetter alias, ITalkingActivatorGetter tact)
        => WithDisplayName(quest, alias, speakerFactory.Create(tact));

    private Speaker WithDisplayName(IQuestGetter quest, IQuestAliasGetter alias, Speaker speaker)
    {
        if (alias.DisplayName.TryResolve(env, out IMessageGetter? displayName) && displayName.Name is not null)
        {
            var (english, japanese) = displayName.Name;

            logger.Debug("{@Quest} alias {QuestAlias} replaces {@Speaker}'s name with {DisplayName}.",
                quest, alias.ID, speaker, english);

            return speaker with
            {
                EnglishName = english,
                JapaneseName = japanese
            };
        }

        return speaker;
    }
}
