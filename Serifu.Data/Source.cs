using System.ComponentModel;

namespace Serifu.Data;

public enum Source : short
{
    Kancolle,
    Skyrim,
    [Description("Witcher 3")]
    Witcher3,
    [Description("G-senjou no Maou")]
    GSenjouNoMaou
}
