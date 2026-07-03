# Build context is the repository root (see docker-compose.yml) so that the
# solution-wide props (Directory.Build.props / Directory.Packages.props) are
# available during restore under Central Package Management.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Npgsql probes for the Kerberos/GSSAPI library during authentication. The minimal
# .NET runtime image doesn't ship it, which logs a noisy (harmless) error on startup.
# Install it as root before dropping to the non-root app user so the image is complete.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the manifests first for a cache-friendly restore layer.
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/MatchService/MatchService.csproj", "src/MatchService/"]
RUN dotnet restore "src/MatchService/MatchService.csproj"

COPY . .
RUN dotnet publish "src/MatchService/MatchService.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MatchService.dll"]
