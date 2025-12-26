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

using Microsoft.AspNetCore.Mvc;
using Serifu.Web.Helpers;
using Serifu.Web.Models;
using System.Text.Json;

namespace Serifu.Web.Controllers;

public class ManifestController : Controller
{
    private readonly ViteAssetProvider assets;

    public ManifestController(ViteAssetProvider assets)
    {
        this.assets = assets;
    }

    [HttpGet("/manifest.json")]
    public ContentResult Manifest()
    {
        WebManifest manifest = new()
        {
            Name = "Serifu.org",
            StartUrl = "/",
            Display = "standalone",
            BackgroundColor = "#111111",
            ThemeColor = "#111111",
            Icons = [
                new()
                {
                    Src = assets["images/favicon@180.png"],
                    Sizes = "180x180",
                    Type = "image/png",
                },
                new()
                {
                    Src = assets["images/favicon.svg"],
                    Sizes = "any",
                    Type = "image/svg+xml",
                },
                new()
                {
                    Src = assets["images/favicon-maskable@192.png"],
                    Sizes = "192x192",
                    Type = "image/png",
                    Purpose = "maskable",
                },
                new()
                {
                    Src = assets["images/favicon-maskable@512.png"],
                    Sizes = "512x512",
                    Type = "image/png",
                    Purpose = "maskable",
                },
                new()
                {
                    Src = assets["images/favicon-maskable.svg"],
                    Sizes = "any",
                    Type = "image/svg+xml",
                    Purpose = "maskable",
                },
            ],
        };

        return new()
        {
            Content = JsonSerializer.Serialize(manifest, WebManifestSerializerContext.Default.Options),
            ContentType = "application/manifest+json; charset=utf-8",
        };
    }
}
