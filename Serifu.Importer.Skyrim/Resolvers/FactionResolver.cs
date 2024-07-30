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

using DotNext.Collections.Generic;
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace Serifu.Importer.Skyrim.Resolvers;

internal class FactionResolver
{
    private readonly Dictionary<FormKey, HashSet<Speaker>> factionToNpcs = [];
    private readonly Dictionary<FormKey, HashSet<Speaker>> factionOverrideAdditionalNpcs = [];

    public FactionResolver(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        ISpeakerFactory speakerFactory,
        IOptions<SkyrimOptions> options,
        ILogger logger)
    {
        logger = logger.ForContext<FactionResolver>();

        using (logger.BeginTimedOperation("Indexing faction NPCs"))
        {
            foreach (var npc in env.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var speaker = speakerFactory.Create(npc);

                foreach (var npcFaction in npc.Factions)
                {
                    if (!npcFaction.Faction.TryResolve(env, out IFactionGetter? faction))
                    {
                        continue;
                    }

                    factionToNpcs.GetOrAdd(faction.FormKey, []).Add(speaker);
                }
            }

            // Will throw if any of the faction editor IDs or NPC FormKeys are invalid
            foreach (var (editorId, values) in options.Value.FactionOverrides)
            {
                IFactionGetter faction = env.LinkCache.Resolve<IFactionGetter>(editorId);
                HashSet<Speaker> factionNpcs = factionToNpcs.GetValueOrDefault(faction.FormKey) ?? [];

                foreach (var value in values)
                {
                    if (!value.Contains(':'))
                    {
                        continue;
                    }

                    var npc = env.LinkCache.Resolve<INpcGetter>(FormKey.Factory(value));
                    var speaker = speakerFactory.Create(npc);

                    if (!factionNpcs.Contains(speaker))
                    {
                        logger.Debug("Faction override: Adding {@Npc} to {@Faction}", npc, faction);
                        factionOverrideAdditionalNpcs.GetOrAdd(faction.FormKey, []).Add(speaker);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds all NPCs in the faction. Note: Faction rank is currently ignored.
    /// </summary>
    /// <param name="faction">The faction.</param>
    /// <param name="includeFactionOverrides">
    /// If a faction override exists, and it includes NPCs referenced by FormKey that are not present in the faction,
    /// whether to add those NPCs to the result set.
    /// </param>
    /// <returns>
    /// A result containing the faction members and the faction editor ID added to <see
    /// cref="SpeakersResult.Factions"/>.
    /// </returns>
    public SpeakersResult Resolve(IFactionGetter faction, bool includeFactionOverrides = true)
    {
        IEnumerable<Speaker> npcs = factionToNpcs.GetValueOrDefault(faction.FormKey, []);

        if (includeFactionOverrides &&
            factionOverrideAdditionalNpcs.TryGetValue(faction.FormKey, out var additionalNpcs))
        {
            npcs = npcs.Concat(additionalNpcs);
        }

        return new SpeakersResult(npcs, faction.EditorID is null ? [] : [faction.EditorID]);
    }
}
