# Multi-stage Dockerfile for TheThroneOfGames.API
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["TheThroneOfGames.API/TheThroneOfGames.API.csproj", "TheThroneOfGames.API/"]
COPY ["TheThroneOfGames.Application/TheThroneOfGames.Application.csproj", "TheThroneOfGames.Application/"]
COPY ["TheThroneOfGames.Domain/TheThroneOfGames.Domain.csproj", "TheThroneOfGames.Domain/"]
COPY ["TheThroneOfGames.Infrastructure/TheThroneOfGames.Infrastructure.csproj", "TheThroneOfGames.Infrastructure/"]
COPY ["GameStore.Usuarios/GameStore.Usuarios.csproj", "GameStore.Usuarios/"]
COPY ["GameStore.Catalogo/GameStore.Catalogo.csproj", "GameStore.Catalogo/"]
COPY ["GameStore.Vendas/GameStore.Vendas.csproj", "GameStore.Vendas/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]

# Restore dependencies
RUN dotnet restore "TheThroneOfGames.API/TheThroneOfGames.API.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/TheThroneOfGames.API"
RUN dotnet build "TheThroneOfGames.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "TheThroneOfGames.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/api/usuario/public-info || exit 1

# Expose port
EXPOSE 80

# Switch to non-root user
USER appuser

# Run application
ENTRYPOINT ["dotnet", "TheThroneOfGames.API.dll"]
