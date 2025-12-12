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

using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.Mapping;
using Mutagen.Bethesda.Strings;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Serifu.Importer.Skyrim;

internal static class MutagenExtensions
{
    private static readonly ConcurrentDictionary<Type, string> recordTypeMap = [];

    /// <summary>
    /// Registers <see cref="IGameEnvironment{TModSetter, TModGetter}"/> and <see cref="IGameEnvironment"/>.
    /// </summary>
    /// <typeparam name="TMod">The game's mod setter interface.</typeparam>
    /// <typeparam name="TModGetter">The game's mod getter interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="gameRelease">The game release.</param>
    /// <param name="options">An action to configure the game environment.</param>
    public static IServiceCollection AddMutagen<TMod, TModGetter>(
        this IServiceCollection services,
        GameRelease gameRelease,
        Action<IServiceProvider, GameEnvironmentBuilder<TMod, TModGetter>>? options = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        services.AddSingleton(provider =>
        {
            var builder = GameEnvironment.Typical.Builder<TMod, TModGetter>(gameRelease);
            options?.Invoke(provider, builder);
            return builder.Build();
        });

        services.AddSingleton<IGameEnvironment>(provider =>
            provider.GetRequiredService<IGameEnvironment<TMod, TModGetter>>());

        return services;
    }

    /// <summary>
    /// Attempts to locate the link's winning target record.
    /// </summary>
    /// <remarks>
    /// Avoids an ambiguous method overload when using Mutagen's TryResolve() with an explicit (non-<see
    /// langword="var"/>) type for <paramref name="record"/>.
    /// </remarks>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="formLink">The (possibly-null) form link.</param>
    /// <param name="env">The game environment whose link cache to use.</param>
    /// <param name="record">The resolved record.</param>
    /// <returns><see langword="true"/> if the record was successfully resolved; <see langword="false"/> if the form
    /// reference is null or invalid.</returns>
    public static bool TryResolve<T>(this IFormLinkGetter<T> formLink, IGameEnvironment env, [NotNullWhen(true)] out T? record)
        where T : class, IMajorRecordGetter
    {
        return formLink.TryResolve<T>(env.LinkCache, out record);
    }

    /// <summary>
    /// Attempts to locate the link's winning target record.
    /// </summary>
    /// <remarks>
    /// Simplifies record resolution by returning <see langword="null"/> when the form reference is null or invalid,
    /// rather than throwing an exception as Mutagen's Resolve() does.
    /// </remarks>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="formLink">The (possibly-null) form link.</param>
    /// <param name="env">The game environment whose link cache to use.</param>
    /// <returns>The resolved record, or <see langword="null"/> if the form reference is null or invalid.</returns>
    public static T? Resolve<T>(this IFormLinkGetter<T> formLink, IGameEnvironment env)
        where T : class, IMajorRecordGetter
    {
        formLink.TryResolve(env.LinkCache, out var record);
        return record;
    }

    /// <summary>
    /// Gets the usual four-character record type, e.g. <c>QUST</c>.
    /// </summary>
    /// <param name="record">A record.</param>
    /// <exception cref="ArgumentException">The record's registered getter type is missing the <see
    /// cref="AssociatedRecordTypesAttribute"/>.</exception>
    public static string GetRecordType(this IMajorRecordGetter record)
    {
        Type getterType = record.Registration.GetterType;

        return recordTypeMap.GetOrAdd(getterType, t =>
            t.GetCustomAttribute<AssociatedRecordTypesAttribute>()?.Types.First().Type
            ?? throw new ArgumentException($"{getterType.Name} does not have {nameof(AssociatedRecordTypesAttribute)}.", nameof(record)));
    }

    /// <summary>
    /// Converts the <see cref="FormID"/> to an eight-character hex string.
    /// </summary>
    /// <remarks>
    /// Mutagen removed the AsHex() method in an update, and the new <see cref="FormID.ToString"/> doesn't pad to the
    /// expected eight characters. This probably wouldn't be needed if Serilog supported custom scalar converters.
    /// </remarks>
    /// <param name="formId">The form ID.</param>
    public static string AsHex(this FormID formId) => formId.Raw.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Deconstructs a translated string into a tuple of (English, Japanese). Nulls are replaced with empty string.
    /// </summary>
    /// <param name="str">A translated string, or null.</param>
    /// <param name="english">The English string, or empty string if none.</param>
    /// <param name="japanese">The Japanese string, or empty string if none.</param>
    public static void Deconstruct(this ITranslatedStringGetter? str, out string english, out string japanese)
    {
        english = str is not null && str.TryLookup(Language.English, out string? en) ? en : "";
        japanese = str is not null && str.TryLookup(Language.Japanese, out string? ja) ? ja : "";
    }

    /// <summary>
    /// Configures Serilog to render records and form links as a formatted form ID string in the same style as xEdit,
    /// including the editor ID, name, and record type. Unlike the default ToString(), this logs the full
    /// eight-character hex including mod index for easy copy-pasting into xEdit.
    /// </summary>
    /// <remarks>
    /// For the custom destructuring policy to take effect over the default ToString(), the property needs to be
    /// explicitly destructured, e.g. <c>{@Topic}</c>. Serilog unfortunately doesn't provide a way to add custom scalar
    /// conversion policies.
    /// </remarks>
    /// <param name="config">The logger config.</param>
    /// <param name="provider">The service provider with an <see cref="IFormIdProvider"/> registered.</param>
    public static LoggerConfiguration FormattedFormIds(this LoggerDestructuringConfiguration config, IServiceProvider provider) =>
        config.With(new FormIdDestructuringPolicy(provider.GetRequiredService<IFormIdProvider>()));

    private sealed class FormIdDestructuringPolicy(IFormIdProvider formIdProvider) : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
        {
            if (value is not IFormLinkIdentifier formLink)
            {
                result = null;
                return false;
            }

            string str = formIdProvider.GetFormattedString(formLink);
            result = propertyValueFactory.CreatePropertyValue(str);
            return true;
        }
    }
}
