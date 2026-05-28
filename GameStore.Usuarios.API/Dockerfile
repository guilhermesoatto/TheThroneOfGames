# Build stage — SDK pinned to immutable SHA digest (amd64)
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:0d2d99c1f384a6b9c8f37aaea952937b2ffff20aa150c7eb4fdeb0a968797d31 AS build
WORKDIR /src

# Copy project files
COPY ["GameStore.Usuarios.API/GameStore.Usuarios.API.csproj", "GameStore.Usuarios.API/"]
COPY ["GameStore.Usuarios/GameStore.Usuarios.csproj", "GameStore.Usuarios/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
COPY ["TheThroneOfGames.Application/TheThroneOfGames.Application.csproj", "TheThroneOfGames.Application/"]
COPY ["TheThroneOfGames.Domain/TheThroneOfGames.Domain.csproj", "TheThroneOfGames.Domain/"]
COPY ["TheThroneOfGames.Infrastructure/TheThroneOfGames.Infrastructure.csproj", "TheThroneOfGames.Infrastructure/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]

# Restore dependencies
RUN dotnet restore "GameStore.Usuarios.API/GameStore.Usuarios.API.csproj"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "GameStore.Usuarios.API/GameStore.Usuarios.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GameStore.Usuarios.API/GameStore.Usuarios.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — ASP.NET runtime pinned to immutable SHA digest (amd64)
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:906fe6afa26ebfb013a769a659a5bc1eb30424152bcec3c6cd8b0bd88dd69d1c AS final
WORKDIR /app

# Install curl (minimal — required for HEALTHCHECK only)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user (uid 1000)
RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser

# Copy published application with correct ownership
COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GameStore.Usuarios.API.dll"]
