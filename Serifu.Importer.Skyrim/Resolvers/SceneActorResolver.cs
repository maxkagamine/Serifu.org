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

    /// <summary>
    /// Checks if the dialogue topic is references by a scene and resolves the quest alias identified by the scene
    /// dialogue action's actor ID if so.
    /// </summary>
    /// <param name="topic">The dialogue topic.</param>
    /// <returns>
    /// The scene actor, or empty if not scene dialogue or the quest alias did not point to any specific NPC. May
    /// contain multiple speakers if the alias is filled using match conditions.
    /// </returns>
    public SpeakersResult Resolve(IDialogTopicGetter topic)
    {
        if (!dialogTopicToSceneAction.TryGetValue(topic.FormKey, out var sceneAction))
        {
            if (topic.SubtypeName == SubtypeName.SceneDialogueAction)
            {
                logger.Warning("{@Topic} has subtype name SCEN but no scene references it.", topic);
            }

            return SpeakersResult.Empty;
        }

        (ISceneGetter scene, ISceneActionGetter action, int index) = sceneAction;

        if (action.ActorID is not int actorId)
        {
            logger.Warning("{@Topic} is used by Action #{SceneAction} in {@Scene} but it does not have an Actor ID.",
                topic, index, scene);

            return SpeakersResult.Empty;
        }

        if (!scene.Quest.TryResolve(env, out IQuestGetter? quest))
        {
            logger.Warning("{@Topic} is used by Action #{SceneAction} in {@Scene} but it has a null or invalid quest reference.",
                topic, index, scene);

            return SpeakersResult.Empty;
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
