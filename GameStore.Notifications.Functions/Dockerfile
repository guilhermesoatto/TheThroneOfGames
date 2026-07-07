# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["GameStore.Notifications.Functions/GameStore.Notifications.Functions.csproj", "GameStore.Notifications.Functions/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]

# Restore dependencies
RUN dotnet restore "GameStore.Notifications.Functions/GameStore.Notifications.Functions.csproj"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "GameStore.Notifications.Functions/GameStore.Notifications.Functions.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GameStore.Notifications.Functions/GameStore.Notifications.Functions.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — imagem oficial do Azure Functions Worker isolated para .NET 9
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
WORKDIR /home/site/wwwroot

COPY --from=publish /app/publish .
