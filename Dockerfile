# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "TheThroneOfGames.API.dll"]
