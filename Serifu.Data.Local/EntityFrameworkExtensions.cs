using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Serifu.Data.Local;
internal static class EntityFrameworkExtensions
{
    /// <summary>
    /// Sets the values and navigation properties of this entity by copying from the given object.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entry">The change tracking entry for the tracked entity whose values and navigation properties to replace.</param>
    /// <param name="obj">The detached entity whose values and navigation properties to copy.</param>
    public static void SetValuesAndNavigations<T>(this EntityEntry<T> entry, T obj) where T : class
    {
        entry.CurrentValues.SetValues(obj);

        foreach (var navigation in entry.Navigations)
        {
            navigation.CurrentValue = entry.Context.Entry(obj).Navigations
                .Single(n => n.Metadata.Name == navigation.Metadata.Name)
                .CurrentValue;
        }
    }

    /// <summary>
    /// Updates <paramref name="entity"/> by setting its values and navigation properties to those of <paramref
    /// name="valuesFrom"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="set">The <see cref="DbSet{TEntity}"/>.</param>
    /// <param name="entity">The tracked entity whose values and navigation properties to replace.</param>
    /// <param name="valuesFrom">The detached entity whose values and navigation properties to copy.</param>
    public static void Update<T>(this DbSet<T> set, T entity, T valuesFrom) where T : class
    {
        set.Entry(entity).SetValuesAndNavigations(valuesFrom);
    }
}
