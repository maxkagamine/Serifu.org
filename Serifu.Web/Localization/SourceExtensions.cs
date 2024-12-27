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

using Serifu.Data;

namespace Serifu.Web.Localization;

public static class SourceExtensions
{
    public static string GetLocalizedTitle(this Source source) =>
        Strings.GetResourceString($"SourceTitle_{source}") ?? throw new Exception($"No localized title for {source}.");

    public static string GetLocalizedUrl(this Source source) =>
        Strings.GetResourceString($"SourceUrl_{source}") ?? throw new Exception($"No localized title for {source}.");
}
