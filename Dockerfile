# Build and package the bot. The importer is a developer tool and is deliberately not shipped.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, against the project files alone, so a code-only change reuses the layer.
# .editorconfig carries the analyzer severities the build depends on, so it must be here too:
# without it the Release build fails on rules the repository suppresses.
COPY Directory.Build.props Directory.Packages.props .editorconfig Stanley.sln ./
COPY src/Stanley.Core/Stanley.Core.csproj src/Stanley.Core/
COPY src/Stanley.Data/Stanley.Data.csproj src/Stanley.Data/
COPY src/Stanley.Bot/Stanley.Bot.csproj src/Stanley.Bot/
RUN dotnet restore src/Stanley.Bot/Stanley.Bot.csproj

COPY src/ src/
RUN dotnet publish src/Stanley.Bot/Stanley.Bot.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# The Debian-based runtime image carries ICU, which NameNormalizer needs to fold accents.
# An Alpine image would need icu-libs installed explicitly.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Runs as a non-root user: the image ships one, and the bot needs no write access.
USER $APP_UID

ENTRYPOINT ["dotnet", "Stanley.Bot.dll"]
