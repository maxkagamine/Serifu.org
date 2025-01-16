# syntax=docker/dockerfile:1-labs
# check=skip=FromPlatformFlagConstDisallowed,InvalidDefaultArgInFrom

ARG DOTNET_VERSION=9.0
ARG ES_VERSION

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS dotnet

WORKDIR /src

COPY --exclude=Serifu.db . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=secret,id=nugetconfig \
    dotnet publish Serifu.Data.Elasticsearch.Build \
        --configfile /run/secrets/nugetconfig \
        -c Release -r linux-x64 -v normal -o /builder

# Build index under x64 since ES doesn't run under QEMU
FROM --platform=linux/amd64 \
    docker.elastic.co/elasticsearch/elasticsearch-wolfi:$ES_VERSION AS index

RUN bin/elasticsearch-plugin install \
        analysis-icu \
        analysis-kuromoji

RUN --mount=type=bind,from=dotnet,source=/builder,target=/builder \
    --mount=type=bind,source=Serifu.db,target=/Serifu.db \
    /builder/Serifu.Data.Elasticsearch.Build

# Final image
FROM docker.elastic.co/elasticsearch/elasticsearch-wolfi:$ES_VERSION

COPY --from=index --parents \
    /usr/share/elasticsearch/plugins /usr/share/elasticsearch/data /

COPY docker/config /usr/share/elasticsearch/config

HEALTHCHECK --start-period=60s --start-interval=1s \
  CMD curl -f localhost:9200/_cluster/health?wait_for_status=green || exit 1
