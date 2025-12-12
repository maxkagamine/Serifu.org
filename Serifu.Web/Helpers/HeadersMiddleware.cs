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

namespace Serifu.Web.Helpers;

internal sealed class HeadersMiddleware : IMiddleware
{
    private readonly Action<IHeaderDictionary> configureHeaders;

    public HeadersMiddleware(Action<IHeaderDictionary> configureHeaders)
    {
        this.configureHeaders = configureHeaders;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(() =>
        {
            configureHeaders(context.Response.Headers);
            return Task.CompletedTask;
        });

        await next(context);
    }
}

internal static class HeadersMiddlewareExtensions
{
    public static IServiceCollection AddHeaders(this IServiceCollection services, Action<IHeaderDictionary> configureHeaders) =>
        services.AddSingleton<HeadersMiddleware>().AddSingleton(configureHeaders);

    public static IApplicationBuilder UseHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<HeadersMiddleware>();
}
