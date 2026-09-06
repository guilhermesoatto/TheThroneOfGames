# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer-cached)
COPY ["TheThroneOfGames.API/TheThroneOfGames.API.csproj", "TheThroneOfGames.API/"]
COPY ["TheThroneOfGames.Domain/TheThroneOfGames.Domain.csproj", "TheThroneOfGames.Domain/"]
COPY ["TheThroneOfGames.Application/TheThroneOfGames.Application.csproj", "TheThroneOfGames.Application/"]
COPY ["TheThroneOfGames.Infrastructure/TheThroneOfGames.Infrastructure.csproj", "TheThroneOfGames.Infrastructure/"]

RUN dotnet restore "TheThroneOfGames.API/TheThroneOfGames.API.csproj"

# Copy source and build
COPY TheThroneOfGames.API/ TheThroneOfGames.API/
COPY TheThroneOfGames.Domain/ TheThroneOfGames.Domain/
COPY TheThroneOfGames.Application/ TheThroneOfGames.Application/
COPY TheThroneOfGames.Infrastructure/ TheThroneOfGames.Infrastructure/

WORKDIR "/src/TheThroneOfGames.API"
RUN dotnet build "TheThroneOfGames.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TheThroneOfGames.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# curl é exigido pelo HEALTHCHECK do docker-compose.yml (curl -f .../api/usuario/public-info)
# — a imagem base não o inclui por padrão, então sem isso o healthcheck falha sempre
# (reportando "unhealthy" mesmo com a API respondendo normalmente).
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Usuário não-root: a imagem base .NET 10 já provê o usuário/grupo "app" (UID 1654).
# (Debian 13 removeu o wrapper `adduser` das imagens mínimas — daí não recriamos o usuário.)
RUN chown -R app:app /app
USER app

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "TheThroneOfGames.API.dll"]
