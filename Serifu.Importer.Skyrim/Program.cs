using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
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
builder.Services.AddSingleton<SceneActorResolver>();
builder.Services.AddSingleton<QuestAliasResolver>();

builder.Run((
    IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
    SceneActorResolver sceneActorResolver,
    ConditionsResolver conditionsResolver,
    IFormIdProvider formIdProvider,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Load order:\n{LoadOrder}", formIdProvider.PrintLoadOrder());

    using (logger.BeginTimedOperation("Processing dialogue"))
    {
        foreach (var topic in env.LoadOrder.PriorityOrder.DialogTopic().WinningOverrides())
        {
            logger.Information("Processing topic {@Topic}", topic);

            IQuestGetter? quest = topic.Quest.Resolve(env);
            IReadOnlyList<IConditionGetter> questDialogueConditions = quest?.DialogConditions ?? [];

            using (LogContext.PushProperty("Topic", topic, true))
            using (LogContext.PushProperty("TopicQuest", quest, true))
            {
                SpeakersResult sceneActorResult = sceneActorResolver.Resolve(topic);

                foreach (IDialogInfoGetter info in topic.Responses)
                {
                    using (LogContext.PushProperty("Info", info, true))
                    {
                        SpeakersResult conditionsResult = conditionsResolver.Resolve(
                            sceneActorResult, questDialogueConditions, info.Conditions);
                    }
                }
            }
        }
    }
});
