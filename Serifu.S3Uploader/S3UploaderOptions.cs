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

using System.ComponentModel.DataAnnotations;

namespace Serifu.S3Uploader;

internal sealed class S3UploaderOptions
{
    /// <summary>
    /// The IAM user's access key ID (set in user secrets).
    /// See https://docs.aws.amazon.com/sdkref/latest/guide/access-iam-users.html.
    /// </summary>
    [Required]
    public string AccessKeyId { get; set; } = "";

    /// <summary>
    /// The IAM user's secret access key (set in user secrets).
    /// See https://docs.aws.amazon.com/sdkref/latest/guide/access-iam-users.html.
    /// </summary>
    [Required]
    public string SecretAccessKey { get; set; } = "";

    /// <summary>
    /// The region name. Not used if <see cref="EndpointUrl"/> is also set.
    /// </summary>
    [Required]
    public string Region { get; set; } = "";

    /// <summary>
    /// The S3 endpoint URL, used for testing against s3mock. Takes precedence over <see cref="Region"/>.
    /// </summary>
    public string EndpointUrl { get; set; } = "";

    /// <summary>
    /// The name of the audio file bucket.
    /// </summary>
    [Required]
    public string AudioBucket { get; set; } = "";
}
