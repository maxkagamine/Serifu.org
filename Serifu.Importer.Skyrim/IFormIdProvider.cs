using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.Mapping;
using System.Text;

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
    /// Returns a formatted form ID string in the same style as xEdit, including the editor ID and record type. Useful
    /// for logging and debugging.
    /// </summary>
    /// <param name="record"></param>
    /// <exception cref="KeyNotFoundException">The <see cref="ModKey"/> does not exist in the load order.</exception>
    /// <exception cref="ArgumentException">The record's registered getter type is missing the <see
    /// cref="AssociatedRecordTypesAttribute"/>.</exception>
    string GetFormattedString(IMajorRecordGetter record)
    {
        StringBuilder sb = new();

        if (record.EditorID is not null)
        {
            sb.Append(record.EditorID);
            sb.Append(' ');
        }

        sb.Append($"[{record.GetRecordType()}:{GetFormId(record.FormKey).Raw.ToString("X8")}]");
        return sb.ToString();
    }

    /// <summary>
    /// Gets a formatted load order with form ID prefixes for display.
    /// </summary>
    string PrintLoadOrder();
}
