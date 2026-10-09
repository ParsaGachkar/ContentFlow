# ContentFlow Web (Blazor) image.
#
# Build stage uses the .NET SDK plus Node.js: the Tailwind/DaisyUI pipeline is
# wired into `dotnet publish` via the Web csproj targets (RestoreClientAssets /
# BuildClientAssets), so the SDK image must provide npm. Never SkipCssBuild here.

# SDK image pinned: must match global.json (10.0.401).
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
# Node.js 22 LTS via NodeSource: Debian's stock nodejs (v18) is below the
# package.json engines floor (>=20) and mishandles lockfile optional
# dependencies (missing tailwindcss native binding). Matches CI setup-node.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" > /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/* \
    && node --version && npm --version
WORKDIR /src
COPY ContentFlow.slnx ./
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
COPY tools/ tools/
RUN dotnet restore src/ContentFlow.Blazor/Web/ContentFlow.Blazor.Web.csproj
RUN dotnet publish src/ContentFlow.Blazor/Web/ContentFlow.Blazor.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContentFlow.Blazor.Web.dll"]

