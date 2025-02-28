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

using System.ComponentModel;

namespace Serifu.Data;

public enum Source : short
{
    Kancolle,
    Skyrim,
    [Description("Witcher 3")]
    Witcher3,
    [Description("G-senjou no Maou")]
    GSenjouNoMaou,
    [Description("Nekopara Vol. 1")]
    NekoparaVol1,
    [Description("Nekopara Vol. 2")]
    NekoparaVol2,
    Maitetsu,
    [Description("Senren＊Banka")]
    SenrenBanka,
    [Description("Newton to Ringo no Ki")]
    NewtonToRingoNoKi,
    [Description("Baldur's Gate 3")]
    BaldursGate3,
    [Description("Go! Go! Nippon!")]
    GoGoNippon,
    [Description("Steins;Gate")]
    SteinsGate,
}
