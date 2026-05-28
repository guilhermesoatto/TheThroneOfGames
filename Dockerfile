# Build stage — SDK pinned to immutable SHA digest (amd64)
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:0d2d99c1f384a6b9c8f37aaea952937b2ffff20aa150c7eb4fdeb0a968797d31 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["TheThroneOfGames.sln", "."]
COPY ["TheThroneOfGames.API/TheThroneOfGames.API.csproj", "TheThroneOfGames.API/"]
COPY ["TheThroneOfGames.Domain/TheThroneOfGames.Domain.csproj", "TheThroneOfGames.Domain/"]
COPY ["TheThroneOfGames.Application/TheThroneOfGames.Application.csproj", "TheThroneOfGames.Application/"]
COPY ["TheThroneOfGames.Infrastructure/TheThroneOfGames.Infrastructure.csproj", "TheThroneOfGames.Infrastructure/"]
COPY ["GameStore.Usuarios/GameStore.Usuarios.csproj", "GameStore.Usuarios/"]
COPY ["GameStore.Catalogo/GameStore.Catalogo.csproj", "GameStore.Catalogo/"]
COPY ["GameStore.Vendas/GameStore.Vendas.csproj", "GameStore.Vendas/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]
COPY ["Test/Test.csproj", "Test/"]
COPY ["GameStore.Usuarios.Tests/GameStore.Usuarios.Tests.csproj", "GameStore.Usuarios.Tests/"]
COPY ["GameStore.Catalogo.Tests/GameStore.Catalogo.Tests.csproj", "GameStore.Catalogo.Tests/"]
COPY ["GameStore.Common.Tests/GameStore.Common.Tests.csproj", "GameStore.Common.Tests/"]

RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR "/src/TheThroneOfGames.API"
RUN dotnet build "TheThroneOfGames.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TheThroneOfGames.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — ASP.NET runtime pinned to immutable SHA digest (amd64)
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:906fe6afa26ebfb013a769a659a5bc1eb30424152bcec3c6cd8b0bd88dd69d1c AS final
WORKDIR /app

# Install curl (minimal — required for HEALTHCHECK only)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user (uid 1000)
RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser \
    && chown -R appuser:appgroup /app

# Copy published application with correct ownership
COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TheThroneOfGames.API.dll"]