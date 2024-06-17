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

using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Data.Elasticsearch;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuElasticsearch(this IServiceCollection services)
    {
        // TODO: ES server defaults to localhost:9200, should get from configuration
        services.AddSingleton<IElasticsearchClientSettings>(new ElasticsearchClientSettings()
#if DEBUG
            .EnableDebugMode()
#endif
            .DefaultIndex("quotes")
            .ThrowExceptions());

        services.AddSingleton<ElasticsearchClient>();

        return services;
    }
}
