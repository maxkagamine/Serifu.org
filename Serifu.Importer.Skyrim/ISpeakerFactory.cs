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
