using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Importer.Skyrim.Resolvers;

// Due to the unavoidable circular dependency of quest alias -> conditions -> quest alias, it's necessary to construct
// one of the two resolvers lazily. This approach is simpler than a factory class.

internal class LazyResolver<T> : Lazy<T>
    where T : notnull
{
    public LazyResolver(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>)
    { }
}
