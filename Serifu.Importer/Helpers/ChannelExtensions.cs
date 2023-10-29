using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Importer.Helpers;
internal static class ChannelExtensions
{
    /// <summary>
    /// Adds an unbounded channel as a singleton to be used as a queue.
    /// </summary>
    /// <typeparam name="T">The queue type.</typeparam>
    public static IServiceCollection AddChannel<T>(this IServiceCollection services)
        => services.AddSingleton(_ => Channel.CreateUnbounded<T>());

    /// <summary>
    /// Asynchronously writes a collection of items to the channel.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="channel">The channel writer.</param>
    /// <param name="items">The values to write to the channel.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the write operation.</param>
    public static async Task WriteRangeAsync<T>(
        this ChannelWriter<T> channel,
        IEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await channel.WriteAsync(item, cancellationToken);
        }
    }
}
