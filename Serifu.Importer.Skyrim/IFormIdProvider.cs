using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.Mapping;

namespace Serifu.Importer.Skyrim;

public interface IFormIdProvider
{
    /// <summary>
    /// Translates a <see cref="FormKey"/> into a <see cref="FormID"/> based on the current load order, as it would be
    /// displayed in game or in xEdit.
    /// </summary>
    /// <param name="formKey">The form key.</param>
    /// <exception cref="KeyNotFoundException">The <see cref="ModKey"/> does not exist in the load order.</exception>
    FormID GetFormId(FormKey formKey);

    /// <inheritdoc cref="GetFormId(FormKey)"/>
    /// <param name="record">The record.</param>
    FormID GetFormId(IFormKeyGetter record) => GetFormId(record.FormKey);

    /// <summary>
    /// Returns a formatted form ID string in the same style as xEdit, including the editor ID, name, and record type.
    /// Supports null and invalid form links. Useful for logging and debugging.
    /// </summary>
    /// <param name="formLink">A record or form link.</param>
    /// <exception cref="KeyNotFoundException">The <see cref="ModKey"/> does not exist in the load order.</exception>
    /// <exception cref="ArgumentException">The record's registered getter type is missing the <see
    /// cref="AssociatedRecordTypesAttribute"/>.</exception>
    string GetFormattedString(IFormLinkIdentifier formLink);

    /// <summary>
    /// Gets a formatted load order with form ID prefixes for display.
    /// </summary>
    string PrintLoadOrder();
}
