using Kagamine.Extensions.Logging;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class SceneActorResolver
{
    private readonly QuestAliasResolver questAliasResolver;
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly ILogger logger;

    private readonly Dictionary<FormKey, (ISceneGetter Scene, ISceneActionGetter Action)> dialogTopicToSceneAction;

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
                    .Where(action => action.Type == SceneAction.TypeEnum.Dialog && !action.Topic.IsNull)
                    .Select(action => (Scene: scene, Action: action)))
                .ToDictionary(x => x.Action.Topic.FormKey);
        }
    }

    public INpcGetter? Resolve(IDialogTopicGetter topic)
    {
        if (!dialogTopicToSceneAction.TryGetValue(topic.FormKey, out var sceneAction))
        {
            if (topic.SubtypeName == SubtypeName.SceneDialogueAction)
            {
                logger.Warning("{@Topic} has subtype name SCEN but no scene references it.", topic);
            }

            return null;
        }

        (ISceneGetter scene, ISceneActionGetter action) = sceneAction;

        if (action.ActorID is not int actorId)
        {
            logger.Warning("{@Scene} does not have an Actor ID for Action #{Index}.", scene, (action.Index ?? 0) - 1);
            return null;
        }

        if (!scene.Quest.TryResolve(env, out IQuestGetter? quest))
        {
            logger.Warning("{@Scene} has a null or invalid quest reference.", scene);
            return null;
        }

        logger.Debug("Found {@Scene} referencing alias {AliasId} in {@Quest}.",
            scene, actorId, quest);

        return questAliasResolver.Resolve(quest, actorId);
    }
}
