# Build context is the repository root (see docker-compose.yml) so the solution-wide
# props (Directory.Build.props / Directory.Packages.props) are available during restore.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the manifests first for a cache-friendly restore layer.
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["src/MatchDayContracts/MatchDayContracts.csproj", "src/MatchDayContracts/"]
RUN dotnet restore "src/NotificationService/NotificationService.csproj"

COPY . .
RUN dotnet publish "src/NotificationService/NotificationService.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
