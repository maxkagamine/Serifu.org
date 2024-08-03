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

using Kagamine.Extensions.Logging;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class UniqueVoiceTypeResolver
{
    // Some NPCs have copies (e.g. GeneralTullius & CWBattleTullius), hence the enumerable
    private readonly Dictionary<FormKey, SpeakersResult> voiceTypeToNpcs;

    public UniqueVoiceTypeResolver(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        ILogger logger)
    {
        logger = logger.ForContext<UniqueVoiceTypeResolver>();

        using (logger.BeginTimedOperation("Indexing unique voice types"))
        {
            voiceTypeToNpcs = env.LoadOrder.PriorityOrder.Npc().WinningOverrides()
                .Select(speakerFactory.Create)
                .Where(s => s.HasVoiceType)
                .GroupBy(s => s.VoiceType)
                .Where(g => g.DistinctBy(s => s.EnglishName).Count() == 1)
                .ToDictionary(
                    g => env.LinkCache.Resolve<IVoiceTypeGetter>(g.Key).FormKey,
                    g => new SpeakersResult(g));

            // Log results
            foreach (var (_, result) in voiceTypeToNpcs)
            {
                var npc = result.First();
                logger.Debug("Found unique voice type {VoiceType} => {Name}", npc.VoiceType, npc.EnglishName);
            }
        }
    }

    /// <summary>
    /// If <paramref name="voiceType"/> is a unique voice type, returns a <see cref="SpeakersResult"/> with the
    /// corresponding NPC (may produce multiple speakers if the NPC has copies); otherwise, returns an empty result.
    /// </summary>
    /// <param name="voiceType">The voice type.</param>
    public SpeakersResult Resolve(IVoiceTypeGetter voiceType) =>
        voiceTypeToNpcs.GetValueOrDefault(voiceType.FormKey) ?? SpeakersResult.Empty;
}
