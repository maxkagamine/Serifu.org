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

builder.Services.AddSingleton<SceneActorResolver>();
builder.Services.AddSingleton<QuestAliasResolver>();

builder.Run((
    IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
    SceneActorResolver sceneActorResolver,
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

            using (LogContext.PushProperty("Topic", topic, true))
            {
                Speaker? sceneActor = sceneActorResolver.Resolve(topic);

                if (sceneActor is not null)
                {
                    logger.Information("Found scene actor {@Speaker} for {@Topic}", sceneActor, topic);
                }

                //foreach (IDialogInfoGetter info in topic.Responses)
                //{
                //    using (LogContext.PushProperty("Info", info, true))
                //    {
                //        INpcGetter[] npcs = sceneActor is null ? [] : [sceneActor];

                //        if (npcs.Length == 0)
                //        {

                //        }
                //    }
                //}
            }
        }
    }
});
