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

namespace Serifu.Importer.Kancolle.Models;

/// <summary>
/// Thrown if a requested wiki page is a redirect. We could follow redirects, but in this case it means we mistakenly
/// followed a link to a Kai, which redirects to the base ship's page and so would result in duplicates.
/// </summary>
internal class WikiRedirectException : Exception
{
    public WikiRedirectException(string from, string to)
        : base($"{from} redirects to {to}.")
    { }
}