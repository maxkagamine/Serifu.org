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
using Mutagen.Bethesda.Plugins;

namespace Serifu.Importer.Skyrim;

public class FormIdProvider : IFormIdProvider
{
    private readonly Dictionary<ModKey, uint> modIndexes = [];
    private readonly string formattedLoadOrder;

    public FormIdProvider(IGameEnvironment env)
    {
        uint nextFullIndex = 0;
        uint nextMediumIndex = 0;
        uint nextLightIndex = 0;

        List<string> formattedLoadOrder = [];

        foreach (var entry in env.LoadOrder.ListedOrder)
        {
            if (!entry.Enabled || entry.Mod is null)
            {
                continue;
            }

            string modIndexStr;
            uint modIndex;

            if (entry.Mod.IsSmallMaster)
            {
                modIndexStr = $"FE {nextLightIndex:X3}";
                modIndex = 0xFE000000 | (nextLightIndex++ << 12);
            }
            else if (entry.Mod.IsMediumMaster) // Introduced in Starfield, will likely be in TES6
            {
                modIndexStr = $"FD {nextMediumIndex:X2}";
                modIndex = 0xFD000000 | (nextMediumIndex++ << 16);
            }
            else
            {
                modIndexStr = nextFullIndex.ToString("X2");
                modIndex = nextFullIndex++ << 24;
            }

            formattedLoadOrder.Add($"{modIndexStr,-6}  {entry.ModKey}");
            modIndexes.Add(entry.ModKey, modIndex);
        }

        this.formattedLoadOrder = string.Join('\n', formattedLoadOrder);
    }

    public FormID GetFormId(FormKey formKey)
    {
        return new FormID(modIndexes[formKey.ModKey] + formKey.ID);
    }

    public string PrintLoadOrder() => formattedLoadOrder;
}
