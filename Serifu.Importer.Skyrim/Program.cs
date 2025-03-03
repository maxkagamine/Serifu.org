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

using FFMpegCore;
using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.IO;
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serifu.Data.Sqlite;
using Serifu.Importer.Skyrim;
using Serifu.Importer.Skyrim.Resolvers;
using Serifu.ML;
using Serilog;

Console.Title = "Skyrim Importer";

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

GlobalFFOptions.Current.BinaryFolder = builder.Configuration["FFMpegBinDirectory"] ?? "";

builder.Services.AddSerifuSerilog((provider, config) => config.Destructure.FormattedFormIds(provider));
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.AddOptions<SkyrimOptions>().BindConfiguration("Skyrim").ValidateDataAnnotations();

builder.Services.AddMutagen<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE, (provider, options) => options
    .WithTargetDataFolder(provider.GetRequiredService<IOptions<SkyrimOptions>>().Value.DataDirectory));

builder.Services.AddTemporaryFileProvider();

builder.Services.AddSingleton<IFormIdProvider, FormIdProvider>();
builder.Services.AddSingleton<ISpeakerFactory, SpeakerFactory>();
builder.Services.AddSingleton<IFuzConverter, FuzConverter>();

builder.Services.AddSingleton<ConditionsResolver>();
builder.Services.AddSingleton<FactionResolver>();
builder.Services.AddSingleton<QuestAliasResolver>();
builder.Services.AddSingleton<SceneActorResolver>();
builder.Services.AddSingleton<UniqueVoiceTypeResolver>();
builder.Services.AddTransient(typeof(Lazy<>), typeof(LazyResolver<>));

builder.Services.AddScoped<SkyrimImporter>();

builder.Run(async (
    SkyrimImporter importer,
    IFormIdProvider formIdProvider,
    ISqliteService sqliteService,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Load order:\n{LoadOrder}", formIdProvider.PrintLoadOrder());

    using (logger.BeginTimedOperation("Import"))
    {
        await importer.Run(cancellationToken);
    }

    await sqliteService.DeleteOrphanedAudioFiles(cancellationToken);
});
