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

using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;

namespace Serifu.Web;

[HtmlTargetElement("img", Attributes = "vite-src")]
public class ViteImageTagHelper : TagHelper
{
    private readonly IViteManifest manifest;
    private readonly IHostEnvironment env;

    public ViteImageTagHelper(IViteManifest manifest, IHostEnvironment env)
    {
        this.manifest = manifest;
        this.env = env;
    }

    [HtmlAttributeName("vite-src")]
    public string? ViteSrc { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("vite-src");

        if (ViteSrc is null)
        {
            return;
        }

        string src = ViteSrc.TrimStart('/');

        if (env.IsDevelopment())
        {
            src = $"/{src}";
        }
        else if (manifest[src] is not IViteChunk chunk)
        {
            throw new Exception($"No chunk in manifest called \"{src}\".");
        }
        else
        {
            src = $"/{chunk.File}";
        }

        output.Attributes.Add("src", src);
    }
}
