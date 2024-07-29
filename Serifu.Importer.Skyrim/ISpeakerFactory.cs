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

using Mutagen.Bethesda.Skyrim;

namespace Serifu.Importer.Skyrim;

internal interface ISpeakerFactory
{
    /// <summary>
    /// Creates a <see cref="Speaker"/> from an NPC.
    /// </summary>
    Speaker Create(INpcGetter npc);

    /// <summary>
    /// Creates a <see cref="Speaker"/> from a TACT.
    /// </summary>
    Speaker Create(ITalkingActivatorGetter tact);

    /// <summary>
    /// If the <paramref name="speaker"/> is an NPC, recurses through the NPC record and its templates until a non-null
    /// value is found for a given property. Otherwise, returns <see langword="default"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="speaker">The speaker.</param>
    /// <param name="selector">The property selector.</param>
    T? GetNpcProperty<T>(Speaker speaker, Func<INpcGetter, T?> selector);
}
