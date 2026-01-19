# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["GameStore.Catalogo.API/GameStore.Catalogo.API.csproj", "GameStore.Catalogo.API/"]
COPY ["GameStore.Catalogo/GameStore.Catalogo.csproj", "GameStore.Catalogo/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
COPY ["TheThroneOfGames.Application/TheThroneOfGames.Application.csproj", "TheThroneOfGames.Application/"]
COPY ["TheThroneOfGames.Domain/TheThroneOfGames.Domain.csproj", "TheThroneOfGames.Domain/"]
COPY ["TheThroneOfGames.Infrastructure/TheThroneOfGames.Infrastructure.csproj", "TheThroneOfGames.Infrastructure/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]

# Restore dependencies
RUN dotnet restore "GameStore.Catalogo.API/GameStore.Catalogo.API.csproj"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "GameStore.Catalogo.API/GameStore.Catalogo.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GameStore.Catalogo.API/GameStore.Catalogo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

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
ENTRYPOINT ["dotnet", "GameStore.Catalogo.API.dll"]
