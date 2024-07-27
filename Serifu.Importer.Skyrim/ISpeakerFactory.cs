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
}
