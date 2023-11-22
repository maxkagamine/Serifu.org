using System.Collections.ObjectModel;

namespace Serifu.Data.Entities;
public class TranslationCollection : KeyedCollection<string, Translation>
{
    protected override string GetKeyForItem(Translation item) => item.Language;
}
