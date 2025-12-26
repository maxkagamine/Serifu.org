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

using Vite.AspNetCore;

namespace Serifu.Web.Helpers;

public sealed class ViteAssetProvider
{
    private readonly IViteManifest manifest;
    private readonly IHostEnvironment env;

    public ViteAssetProvider(IViteManifest manifest, IHostEnvironment env)
    {
        this.manifest = manifest;
        this.env = env;
    }

    /// <summary>
    /// Gets the absolute path of an asset.
    /// </summary>
    /// <param name="src">The src path relative to the Assets directory.</param>
    /// <exception cref="ArgumentException">No chunk in manifest called <paramref name="src"/>.</exception>
    public string this[string src]
    {
        get
        {
            src = src.TrimStart('/');

            if (env.IsDevelopment())
            {
                return $"/{src}";
            }

            if (manifest[src] is not IViteChunk chunk)
            {
                throw new ArgumentException($"No chunk in manifest called \"{src}\".", nameof(src));
            }

            return $"/{chunk.File}";
        }
    }
}
