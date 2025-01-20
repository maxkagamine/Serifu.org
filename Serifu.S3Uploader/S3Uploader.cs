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

using Amazon.S3;
using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.S3Uploader;

public class S3Uploader
{
    private readonly IAmazonS3 s3;
    private readonly S3UploaderOptions options;
    private readonly ILogger logger;

    public S3Uploader(IAmazonS3 s3, IOptions<S3UploaderOptions> options, ILogger logger)
    {
        this.s3 = s3;
        this.options = options.Value;
        this.logger = logger.ForContext<S3Uploader>();
    }

    public Task PreDeploy(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task PostDeploy(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
