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

namespace Serifu.Importer.Skyrim.Resolvers;

internal class QuestAliasResolver
{
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ILogger logger;

    public QuestAliasResolver(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ILogger logger)
    {
        this.env = env;
        this.logger = logger.ForContext<QuestAliasResolver>();
    }

    public INpcGetter? Resolve(IQuestGetter quest, int aliasId)
    {
        INpcGetter? result = Resolve(quest, aliasId, []);

        if (result is null)
        {
            logger.Debug("No NPC found for {@Quest} alias {AliasId}.", quest, aliasId);
        }

        return result;
    }

    private INpcGetter? Resolve(IQuestGetter quest, int aliasId, HashSet<FormKey> processedQuests)
    {
        if (!processedQuests.Add(quest.FormKey))
        {
            logger.Warning("Detected cyclic reference at {@Quest}.", quest);
            return null;
        }

        if (quest.Aliases.SingleOrDefault(a => a.ID == aliasId) is not IQuestAliasGetter alias)
        {
            logger.Warning("Alias {AliasId} not found in {@Quest}.", aliasId, quest);
            return null;
        }
        
        if (alias.CreateReferenceToObject is not null &&
            alias.CreateReferenceToObject.Object.TryResolve(env, out ISkyrimMajorRecordGetter? aliasCreatedObject))
        {
            if (aliasCreatedObject is not INpcGetter aliasCreatedNpc)
            {
                logger.Warning("{@Quest} alias {AliasId} has Create Reference to Object but reference is not to an NPC.",
                    quest, aliasId);
            }
            else
            {
                logger.Debug("Found {@Npc} in {@Quest} alias {AliasId}'s Create Reference to Object.",
                    aliasCreatedNpc, quest, aliasId);

                return aliasCreatedNpc;
            }
        }

        if (alias.ForcedReference.TryResolve(env, out IPlacedGetter? forcedReference))
        {
            if (forcedReference is not IPlacedNpcGetter forcedReferenceNpc)
            {
                logger.Warning("{@Quest} alias {AliasId} has a Forced Reference but reference is not an NPC.",
                    quest, aliasId);
            }
            else if (!forcedReferenceNpc.Base.TryResolve(env, out INpcGetter? forcedReferenceNpcBase))
            {
                logger.Warning("{@Quest} alias {AliasId} has a Forced Reference to {@Reference} which lacks a Base.",
                    quest, aliasId, forcedReferenceNpc);
            }
            else
            {
                logger.Debug("Found {@Npc} in {@Quest} alias {AliasId}'s Forced Reference.",
                    forcedReferenceNpcBase, quest, aliasId);

                return forcedReferenceNpcBase;
            }
        }

        if (alias.UniqueActor.TryResolve(env, out INpcGetter? uniqueActor))
        {
            logger.Debug("Found {@Npc} in {@Quest} alias {AliasId}'s Unique Actor.",
                uniqueActor, quest, aliasId);

            return uniqueActor;
        }

        if (alias.External is not null &&
            alias.External.Quest.TryResolve(env, out IQuestGetter? externalQuest) &&
            alias.External.AliasID is int externalAliasId)
        {
            logger.Debug("Following {@Quest} alias {AliasId}'s External Alias Reference to {@ExternalQuest} alias {ExternalAliasId}.",
                quest, aliasId, externalQuest, externalAliasId);

            return Resolve(externalQuest, externalAliasId, processedQuests);
        }

        return null;
    }
}
