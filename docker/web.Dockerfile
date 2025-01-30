# syntax=docker/dockerfile:1-labs
# check=skip=FromPlatformFlagConstDisallowed

ARG DOTNET_VERSION=9.0
ARG NODE_VERSION=23.6.0

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS build

# Install node
ARG NODE_VERSION
RUN NODE_PACKAGE=node-v${NODE_VERSION}-linux-x64 && \
    wget -qO- https://nodejs.org/download/release/v${NODE_VERSION}/${NODE_PACKAGE}.tar.gz | \
    tar xfz - -C /usr/local --strip-components=1 ${NODE_PACKAGE}/bin ${NODE_PACKAGE}/lib

# Build wwwroot
WORKDIR /src/Serifu.Web
COPY Serifu.Web/package*.json .
RUN npm ci
COPY Serifu.Web/biome.json Serifu.Web/tsconfig.json Serifu.Web/vite.config.ts ./
COPY Serifu.Web/Assets Assets
RUN npx vite build

# Publish
WORKDIR /src
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=secret,id=nugetconfig \
    dotnet publish Serifu.Web \
        --configfile /run/secrets/nugetconfig \
        -c Release -v normal -o /publish /p:UseAppHost=false

RUN mkdir /var/log/seq

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra

ENV ASPNETCORE_HTTP_PORTS=3900
EXPOSE 3900

WORKDIR /app
COPY --from=build /publish .

COPY --from=build --chown=$APP_UID:$APP_UID /var/log/seq /var/log/seq
ENV SeqBufferDirectory=/var/log/seq

ENTRYPOINT ["dotnet", "Serifu.Web.dll"]
