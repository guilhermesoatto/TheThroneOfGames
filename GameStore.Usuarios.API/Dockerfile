# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["GameStore.Usuarios.API/GameStore.Usuarios.API.csproj", "GameStore.Usuarios.API/"]
COPY ["GameStore.Usuarios/GameStore.Usuarios.csproj", "GameStore.Usuarios/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
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

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# curl é exigido pelo HEALTHCHECK (docker-compose.yml sobrescreve o teste abaixo com
# "curl -f http://localhost/health") — a imagem base não o inclui por padrão.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
    CMD curl -f http://localhost/swagger || exit 1

# Run application
ENTRYPOINT ["dotnet", "GameStore.Usuarios.API.dll"]
