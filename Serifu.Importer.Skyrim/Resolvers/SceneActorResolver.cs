using Kagamine.Extensions.Logging;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using Serilog.Context;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class SceneActorResolver
{
    private readonly QuestAliasResolver questAliasResolver;
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ILogger logger;

    private readonly Dictionary<FormKey, (ISceneGetter Scene, ISceneActionGetter Action, int Index)> dialogTopicToSceneAction;

    public SceneActorResolver(
        QuestAliasResolver questAliasResolver,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ILogger logger)
    {
        this.questAliasResolver = questAliasResolver;
        this.env = env;
        this.logger = logger = logger.ForContext<SceneActorResolver>();

        using (logger.BeginTimedOperation("Indexing scene dialogue"))
        {
            dialogTopicToSceneAction = env.LoadOrder.PriorityOrder.Scene().WinningOverrides()
                .SelectMany(scene => scene.Actions
                    .Select((action, i) => (Scene: scene, Action: action, Index: i))
                    .Where(x => x.Action.Type == SceneAction.TypeEnum.Dialog && !x.Action.Topic.IsNull))
                .ToDictionary(x => x.Action.Topic.FormKey);
        }
    }

    public Speaker? Resolve(IDialogTopicGetter topic)
    {
        if (!dialogTopicToSceneAction.TryGetValue(topic.FormKey, out var sceneAction))
        {
            if (topic.SubtypeName == SubtypeName.SceneDialogueAction)
            {
                logger.Warning("{@Topic} has subtype name SCEN but no scene references it.", topic);
            }

            return null;
        }

        (ISceneGetter scene, ISceneActionGetter action, int index) = sceneAction;

        if (action.ActorID is not int actorId)
        {
            logger.Warning("{@Topic} is used by Action #{SceneAction} in {@Scene} but it does not have an Actor ID.",
                topic, index, scene);

            return null;
        }

        if (!scene.Quest.TryResolve(env, out IQuestGetter? quest))
        {
            logger.Warning("{@Topic} is used by Action #{SceneAction} in {@Scene} but it has a null or invalid quest reference.",
                topic, index, scene);

            return null;
        }

        logger.Debug("{@Topic} is used by Action #{SceneAction} in {@Scene} which points to alias {QuestAlias} in {@Quest}.",
            topic, index, scene, actorId, quest);

        using (LogContext.PushProperty("Scene", scene, true))
        using (LogContext.PushProperty("SceneAction", index))
        {
            return questAliasResolver.Resolve(quest, actorId);
        }
    }
}
