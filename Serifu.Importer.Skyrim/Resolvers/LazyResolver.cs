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

namespace Serifu.Importer.Skyrim.Resolvers;

// Due to the unavoidable circular dependency of quest alias -> conditions -> quest alias, it's necessary to construct
// one of the two resolvers lazily. This approach is simpler than a factory class.

internal sealed class LazyResolver<T> : Lazy<T>
    where T : notnull
{
    public LazyResolver(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>)
    { }
}
