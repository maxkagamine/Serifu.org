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

namespace Serifu.Web.Helpers;

[HtmlTargetElement("img", Attributes = "vite-src")]
public sealed class ViteImageTagHelper : TagHelper
{
    private readonly ViteAssetProvider assets;

    public ViteImageTagHelper(ViteAssetProvider assets)
    {
        this.assets = assets;
    }

    [HtmlAttributeName("vite-src")]
    public string? ViteSrc { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViteSrc is null)
        {
            return;
        }

        output.Attributes.RemoveAll("vite-src");
        output.Attributes.Add("src", assets[ViteSrc]);
    }
}
