# syntax=docker/dockerfile:1-labs
# check=skip=FromPlatformFlagConstDisallowed

ARG ES_VERSION=8.17.0

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet

WORKDIR /src

COPY --exclude=Serifu.db . .

ARG TARGETARCH
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=secret,id=nugetconfig \
    dotnet publish Serifu.Data.Elasticsearch.Build \
        --configfile /run/secrets/nugetconfig \
        -c Release -a $TARGETARCH -v normal -o /builder

# Final image
FROM docker.elastic.co/elasticsearch/elasticsearch-wolfi:$ES_VERSION

RUN bin/elasticsearch-plugin install \
        analysis-icu \
        analysis-kuromoji

RUN --mount=type=bind,from=dotnet,source=/builder,target=/builder \
    --mount=type=bind,source=Serifu.db,target=/Serifu.db \
    /builder/Serifu.Data.Elasticsearch.Build

COPY docker/config /usr/share/elasticsearch/config

HEALTHCHECK --start-period=60s --start-interval=1s \
  CMD curl -f localhost:9200/_cluster/health?wait_for_status=green || exit 1
