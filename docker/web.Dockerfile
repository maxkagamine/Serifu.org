# syntax=docker/dockerfile:1-labs

ARG DOTNET_VERSION=9.0
ARG NODE_VERSION=23.6.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS build

# Install node
ARG NODE_VERSION
RUN wget -qO- https://nodejs.org/download/release/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz | \
    tar xfz - -C /usr/local --strip-components=1 node-v${NODE_VERSION}-linux-x64/bin node-v${NODE_VERSION}-linux-x64/lib

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

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra

ENV ASPNETCORE_HTTP_PORTS=3900
EXPOSE 3900

WORKDIR /app
COPY --from=build /publish .

ENTRYPOINT ["dotnet", "Serifu.Web.dll"]
