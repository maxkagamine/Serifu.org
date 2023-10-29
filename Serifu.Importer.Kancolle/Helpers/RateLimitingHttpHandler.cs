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

using System.Threading.RateLimiting;

namespace Serifu.Importer.Kancolle.Helpers;

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
