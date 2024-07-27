using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class QuestAliasResolver
{
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ISpeakerFactory speakerFactory;
    private readonly ILogger logger;

    public QuestAliasResolver(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        ILogger logger)
    {
        this.env = env;
        this.speakerFactory = speakerFactory;
        this.logger = logger.ForContext<QuestAliasResolver>();
    }

    public Speaker? Resolve(IQuestGetter quest, int aliasId)
    {
        Speaker? result = Resolve(quest, aliasId, []);

        if (result is null)
        {
            logger.Debug("No NPC found for {@Quest} alias {QuestAlias}.", quest, aliasId);
        }

        return result;
    }

    private Speaker? Resolve(IQuestGetter quest, int aliasId, HashSet<(FormKey, int)> processedQuestAliases)
    {
        if (!processedQuestAliases.Add((quest.FormKey, aliasId)))
        {
            logger.Warning("Detected cyclic reference at {@Quest} alias {QuestAlias}.", quest, aliasId);
            return null;
        }

        if (quest.Aliases.SingleOrDefault(a => a.ID == aliasId) is not IQuestAliasGetter alias)
        {
            logger.Warning("Alias {QuestAlias} not found in {@Quest}.", aliasId, quest);
            return null;
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
            // TODO: Evaluate quest alias conditions
        }

        if (alias.External is not null &&
            alias.External.Quest.TryResolve(env, out IQuestGetter? externalQuest) &&
            alias.External.AliasID is int externalAliasId)
        {
            logger.Debug("Following {@Quest} alias {QuestAlias}'s External Alias Reference to {@ExternalQuest} alias {ExternalAliasId}.",
                quest, aliasId, externalQuest, externalAliasId);

            return Resolve(externalQuest, externalAliasId, processedQuestAliases);
        }

        return null;
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
