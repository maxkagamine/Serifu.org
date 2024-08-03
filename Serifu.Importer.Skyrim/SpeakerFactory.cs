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

using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;

namespace Serifu.Importer.Skyrim;

internal class SpeakerFactory : ISpeakerFactory
{
    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;

    public SpeakerFactory(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env)
    {
        this.env = env;
    }

    public Speaker Create(INpcGetter npc)
    {
        var (english, japanese) = GetNpcProperty(npc, x => x.Name);
        string voiceType = GetNpcProperty(npc, x => x.Voice.Resolve(env)?.EditorID) ?? "";

        return new Speaker(npc)
        {
            EnglishName = english,
            JapaneseName = japanese,
            VoiceType = voiceType
        };
    }

    public Speaker Create(ITalkingActivatorGetter tact)
    {
        var (english, japanese) = tact.Name;
        string voiceType = tact.VoiceType.Resolve(env)?.EditorID ?? "";

        return new Speaker(tact)
        {
            EnglishName = english,
            JapaneseName = japanese,
            VoiceType = voiceType
        };
    }

    public T? GetNpcProperty<T>(Speaker speaker, Func<INpcGetter, T?> selector)
    {
        if (env.LinkCache.TryResolve(speaker, out var record) && record is INpcGetter npc)
        {
            return GetNpcProperty(npc, selector);
        }

        return default;
    }

    /// <summary>
    /// Recurses through an NPC and its templates until a non-null value is found for a given property.
    /// </summary>
    /// <remarks>
    /// If a template refers to a leveled NPC, the lowest-level (early game) variant is used.
    /// </remarks>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="npcOrLeveledNpc">The NPC.</param>
    /// <param name="selector">The property selector.</param>
    /// <returns>The value, or <see langword="default"/> if not set on the NPC or its templates.</returns>
    private T? GetNpcProperty<T>(INpcSpawnGetter? npcOrLeveledNpc, Func<INpcGetter, T?> selector)
    {
        if (npcOrLeveledNpc is ILeveledNpcGetter leveledNpc)
        {
            var reference = leveledNpc.Entries?
                .Where(x => x.Data is not null)
                .MinBy(x => x.Data!.Level)?
                .Data!.Reference
                .Resolve(env);

            return reference is null ? default : GetNpcProperty(reference, selector);
        }

        if (npcOrLeveledNpc is INpcGetter npc)
        {
            T? value = selector(npc);

            if (value is not null)
            {
                return value;
            }

            if (npc.Template.TryResolve(env, out INpcSpawnGetter? template))
            {
                return GetNpcProperty(template, selector);
            }
        }

        return default;
    }
}
