# ContentFlow Migrator image (explicit DB migrations, independent executable).
#
# No Node.js needed here: only the Web csproj carries the npm/CSS targets.

# SDK image pinned: must match global.json (10.0.401).
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
COPY tools/ tools/
RUN dotnet restore tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj
RUN dotnet publish tools/ContentFlow.Migrator/ContentFlow.Migrator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContentFlow.Migrator.dll"]

