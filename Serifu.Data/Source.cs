﻿using System.ComponentModel;

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
    SenrenBanka
}
