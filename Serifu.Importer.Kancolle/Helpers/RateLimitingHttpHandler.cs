namespace Serifu.Importer.Kancolle.Helpers;

internal class RateLimitingHttpHandler : DelegatingHandler
{
    private static readonly TimeSpan TimeBetweenRequests = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim rateLimiter = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await rateLimiter.WaitAsync(cancellationToken);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeBetweenRequests);
                rateLimiter.Release();
            }, CancellationToken.None);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            rateLimiter.Dispose();
        }

        base.Dispose(disposing);
    }
}
