# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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