using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;

namespace Serifu.Importer.Skyrim;

/// <summary>
/// Represents an NPC (or in rare cases TACT) with name and voice type resolved from the NPC record or its template,
/// recursively. The name may be overridden by a quest alias. Note that the name and/or voice type may be empty; this is
/// to simplify resolver logic, and such instances should be skipped when selecting a speaker from the result set.
/// </summary>
public record Speaker : IFormLinkIdentifier, INamedGetter // Interfaces allow for use with IFormIdProvider
{
    public Speaker(IFormLinkIdentifier formLink)
    {
        FormKey = formLink.FormKey;
        Type = formLink.Type;
    }

    /// <summary>
    /// The NPC (or TACT)'s form key.
    /// </summary>
    public FormKey FormKey { get; }

    /// <summary>
    /// The NPC (or TACT)'s form link type. Required for efficient retrieval from the link cache.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The speaker's resolved English name, or empty string if none.
    /// </summary>
    public required string EnglishName { get; init; }

    /// <summary>
    /// The speaker's resolved Japanese name, or empty string if none.
    /// </summary>
    public required string JapaneseName { get; init; }

    /// <summary>
    /// The speaker's resolved voice type (editor ID), or empty string if none.
    /// </summary>
    public required string VoiceType { get; init; }

    /// <summary>
    /// Whether this speaker has both an English and Japanese name (should be discarded if false).
    /// </summary>
    public bool HasTranslatedName => !string.IsNullOrWhiteSpace(EnglishName) && !string.IsNullOrWhiteSpace(JapaneseName);

    /// <summary>
    /// Whether this speaker has a voice type (should be discarded if false).
    /// </summary>
    public bool HasVoiceType => !string.IsNullOrEmpty(VoiceType);

    string? INamedGetter.Name => EnglishName;

    string INamedRequiredGetter.Name => EnglishName;
}
