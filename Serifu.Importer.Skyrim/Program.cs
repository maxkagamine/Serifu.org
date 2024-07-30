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

using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serifu.Data.Sqlite;
using Serifu.Importer.Skyrim;
using Serifu.Importer.Skyrim.Resolvers;
using Serifu.ML;
using Serilog;
using Serilog.Context;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuSerilog((provider, config) => config.Destructure.FormattedFormIds(provider));
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.Configure<SkyrimOptions>(builder.Configuration.GetSection("Skyrim"));

builder.Services.AddMutagen<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE, (provider, options) => options
    .WithTargetDataFolder(provider.GetRequiredService<IOptions<SkyrimOptions>>().Value.DataDirectory));

builder.Services.AddSingleton<IFormIdProvider, FormIdProvider>();
builder.Services.AddSingleton<ISpeakerFactory, SpeakerFactory>();

builder.Services.AddSingleton<ConditionsResolver>();
builder.Services.AddSingleton<FactionResolver>();
builder.Services.AddSingleton<QuestAliasResolver>();
builder.Services.AddSingleton<SceneActorResolver>();
builder.Services.AddSingleton<UniqueVoiceTypeResolver>();
builder.Services.AddTransient(typeof(Lazy<>), typeof(LazyResolver<>));

builder.Run((
    IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
    SceneActorResolver sceneActorResolver,
    ConditionsResolver conditionsResolver,
    ISpeakerFactory speakerFactory,
    IFormIdProvider formIdProvider,
    IOptions<SkyrimOptions> options,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Load order:\n{LoadOrder}", formIdProvider.PrintLoadOrder());

    // Load BSAs
    VoiceFileArchive englishArchive;
    VoiceFileArchive japaneseArchive;
    using (logger.BeginTimedOperation("Indexing archives"))
    {
        englishArchive = new(options.Value.EnglishVoiceBsaPath, logger);
        japaneseArchive = new(options.Value.JapaneseVoiceBsaPath, logger);
    }

    using (logger.BeginTimedOperation("Processing dialogue"))
    using (var progress = new TerminalProgressBar())
    {
        // Iterate over dialogue topics
        HashSet<RecordType> excludedSubtypes = [
            SubtypeName.Bash,
            SubtypeName.Block,
            SubtypeName.Death,
            SubtypeName.EnterBowZoomBreath,
            SubtypeName.EnterSprintBreath,
            SubtypeName.ExitBowZoomBreath,
            SubtypeName.Hit,
            SubtypeName.OutOfBreath,
            SubtypeName.PowerAttack,
            SubtypeName.VoicePowerEndLong,
            SubtypeName.VoicePowerEndShort,
            SubtypeName.VoicePowerStartLong,
            SubtypeName.VoicePowerStartShort,
        ];

        IDialogTopicGetter[] topics = env.LoadOrder.PriorityOrder.DialogTopic().WinningOverrides()
            .Where(t => !excludedSubtypes.Contains(t.SubtypeName))
            .ToArray();

        for (int i = 0; i < topics.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDialogTopicGetter topic = topics[i];
            logger.Information("Processing topic {@Topic}", topic);
            progress.SetProgress(i, topics.Length);

            // If the topic's quest has dialogue conditions, these are AND'd together with each INFO's conditions. The
            // quest is also used to resolve alias references in conditions.
            IQuestGetter? quest = topic.Quest.Resolve(env);
            IReadOnlyList<IConditionGetter> questDialogueConditions = quest?.DialogConditions ?? [];

            using (LogContext.PushProperty("Topic", topic, true))
            using (LogContext.PushProperty("TopicQuest", quest, true))
            {
                // For scene dialogue, all of the INFOs are spoken by the same actor (usually there's only one, but
                // sometimes a line will change depending on e.g. the player's gender)
                SpeakersResult sceneActorResult = sceneActorResolver.Resolve(topic);

                // Iterate over each INFO in the topic
                foreach (IDialogInfoGetter info in topic.Responses)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.Debug("Processing info {@Info}", info);
                    using (LogContext.PushProperty("Info", info, true))
                    {
                        // Determine the dialogue's speaker based on its conditions, combined with the quest dialogue
                        // conditions. For scene dialogue, this will usually return the scene actor, but it's possible
                        // for the scene actor's quest alias to have multiple possible NPCs which this narrows down.
                        SpeakersResult conditionsResult = conditionsResolver.Resolve(
                            sceneActorResult, quest, questDialogueConditions, info.Conditions);

                        // Remove any that do not have a translated name or voice file (we could do this check for each
                        // response, but presumably if an NPC has voice for one line they'll have them all & vice versa)
                        bool Validate(Speaker speaker)
                        {
                            string? error = null;

                            if (!speaker.HasTranslatedName || !speaker.HasVoiceType)
                            {
                                error = "no translated name and/or voice type";
                            }
                            else if (!info.Responses.All(r => englishArchive.HasVoiceFile(info, r, speaker.VoiceType)))
                            {
                                error = "missing English voice file";
                            }
                            else if (!info.Responses.All(r => japaneseArchive.HasVoiceFile(info, r, speaker.VoiceType)))
                            {
                                error = "missing Japanese voice file";
                            }

                            if (error is not null)
                            {
                                logger.Debug("Removing {@Speaker}: {Reason}", speaker, error);
                                return false;
                            }

                            return true;
                        }

                        SpeakersResult result = new(conditionsResult.Where(Validate), conditionsResult.Factions);

                        // Use INFO's Speaker as fallback if set. This field is mainly used to give a name and voice
                        // type to lines spoken by a TACT or XMarker (usually a daedra), but if the TACT already has
                        // both, it seems the Speaker is unused (example is the "Night Mother Voice NPC" in 0009724E).
                        if (result.IsEmpty && info.Speaker.TryResolve(env, out INpcGetter? infoSpeaker))
                        {
                            logger.Debug("Falling back to INFO's Speaker: {@Speaker}", infoSpeaker);
                            Speaker fallback = speakerFactory.Create(infoSpeaker);

                            if (Validate(fallback))
                            {
                                result = fallback;
                            }
                        }

                        if (result.IsEmpty)
                        {
                            logger.Debug("No speakers found for {@Info}.", info);
                            continue;
                        }

                        logger.Debug("Eligible speakers for {@Info}: {@Speakers}", info, result);

                        // TODO: Exclude dialogue whose Japanese contains neither kanji nor hiragana (dragon language)
                    }
                }
            }
        }
    }
});
