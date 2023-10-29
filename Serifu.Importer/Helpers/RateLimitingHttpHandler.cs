using System.Threading.RateLimiting;

namespace Serifu.Importer.Helpers;

internal class RateLimitingHttpHandler : DelegatingHandler, IAsyncDisposable
{
    private readonly PartitionedRateLimiter<HttpRequestMessage> rateLimiter;

    public RateLimitingHttpHandler()
    {
        rateLimiter = PartitionedRateLimiter.Create<HttpRequestMessage, string>(req =>
            RateLimitPartition.GetFixedWindowLimiter(req.RequestUri?.Host ?? "", _ => new()
            {
                PermitLimit = 1,
                QueueLimit = int.MaxValue,
                Window = TimeSpan.FromSeconds(3),
            }));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await rateLimiter.AcquireAsync(request, cancellationToken: cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    public ValueTask DisposeAsync() => rateLimiter.DisposeAsync();
}
