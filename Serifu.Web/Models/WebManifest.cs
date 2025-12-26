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

namespace Serifu.Web.Models;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class WebManifest
{
    public required string Name { get; set; }

    public required string StartUrl { get; set; }

    public required string Display { get; set; }

    public required string ThemeColor { get; set; }

    public required string BackgroundColor { get; set; }

    public required List<WebManifestIcon> Icons { get; set; }
}

internal sealed class WebManifestIcon
{
    public required string Src { get; set; }

    public required string Sizes { get; set; }

    public required string Type { get; set; }

    public string? Purpose { get; set; }
}

[JsonSerializable(typeof(WebManifest))]
internal sealed partial class WebManifestSerializerContext : JsonSerializerContext
{
    static WebManifestSerializerContext()
    {
        Default = new(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        });
    }
}
