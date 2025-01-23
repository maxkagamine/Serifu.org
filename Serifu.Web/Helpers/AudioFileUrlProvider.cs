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

using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Serifu.Web.Helpers;

public class AudioFileUrlProvider
{
    private readonly AudioFileOptions options;
    private readonly IUrlSigner urlSigner;

    public AudioFileUrlProvider(IOptions<SerifuOptions> options, IUrlSigner urlSigner)
    {
        this.options = options.Value.AudioFiles;
        this.urlSigner = urlSigner;
    }

    /// <summary>
    /// Gets the configured TTL for audio file URLs. May be <see cref="TimeSpan.Zero"/> if URL signing is not enabled.
    /// </summary>
    public TimeSpan Ttl => options.Ttl;

    /// <summary>
    /// Gets the full, signed URL for the given <paramref name="objectName"/>.
    /// </summary>
    /// <param name="objectName">The audio file object name.</param>
    /// <returns>The audio file URL, or <see langword="null"/> if <paramref name="objectName"/> is <see
    /// langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(objectName))]
    public string? GetUrl(string? objectName)
    {
        if (objectName is null)
        {
            return null;
        }

        string url = $"{options.BaseUrl}/{objectName}";
        DateTime expires = DateTime.Now + options.Ttl;

        return urlSigner.SignUrl(url, expires);
    }
}
