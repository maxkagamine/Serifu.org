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

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Kagamine.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serifu.Data.Sqlite;
using Serifu.S3Uploader;
using Serilog;

var builder = ConsoleApplication.CreateBuilder();

builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();

builder.Services.AddOptions<S3UploaderOptions>().BindConfiguration("S3Uploader").ValidateDataAnnotations();

builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var options = provider.GetRequiredService<IOptions<S3UploaderOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger>();

    var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
    var config = new AmazonS3Config();

    if (!string.IsNullOrEmpty(options.EndpointUrl))
    {
        logger.Information("S3 endpoint: {EndpointUrl}", options.EndpointUrl);
        config.ServiceURL = options.EndpointUrl;
        config.ForcePathStyle = true;
    }
    else
    {
        logger.Information("S3 region: {Region}", options.Region);
        config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
    }

    logger.Information("Audio bucket: {AudioBucket}", options.AudioBucket);

    return new AmazonS3Client(credentials, config);
});

builder.Services.AddSingleton<S3Uploader>();

builder.Run(async (S3Uploader uploader, CancellationToken cancellationToken) =>
{
    switch (args.FirstOrDefault())
    {
        case "predeploy":
            Console.Title = "S3 Uploader: Pre-deploy";
            await uploader.PreDeploy(cancellationToken);
            break;
        case "postdeploy":
            Console.Title = "S3 Uploader: Post-deploy";
            await uploader.PostDeploy(cancellationToken);
            break;
        default:
            throw new Exception("Uploader not executed with a valid command.");
    }
});
