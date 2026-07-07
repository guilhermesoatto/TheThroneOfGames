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

# Publish (inclui o build — evita uma segunda compilação redundante)
RUN dotnet publish "GameStore.Notifications.Functions/GameStore.Notifications.Functions.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — imagem oficial do Azure Functions Worker isolated para .NET 9.
# Não existe variante Alpine publicada pela Microsoft para azure-functions/dotnet-isolated
# (confirmado: só há tags Debian bookworm) — mantemos a base Debian oficial e aplicamos
# o mesmo hardening non-root das demais imagens.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
WORKDIR /home/site/wwwroot

RUN groupadd -r appgroup && useradd -r -g appgroup -d /home/site/wwwroot appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

# O host de Functions (runtime nativo da imagem base) precisa escrever em
# /azure-functions-host e /home na inicialização — concede ao usuário non-root
# o necessário sem rodar como root.
#
# setcap: o host escuta em :80 via Kestrel (Microsoft.Azure.WebJobs.Script.WebHost) — non-root
# não pode fazer bind em portas <1024. Diferente de um `docker run` avulso (que herda o
# capability set liberal padrão do Docker Engine, incluindo NET_BIND_SERVICE), o Kubernetes
# via containerd/CRI NÃO concede essa capability por padrão a um pod sem securityContext
# explícito — setcap grava a capability diretamente nos xattrs do binário, funcionando em
# ambos os casos independentemente do capability set herdado do processo pai.
RUN chown -R appuser:appgroup /home /azure-functions-host \
    && apt-get update && apt-get install -y --no-install-recommends libcap2-bin \
    && setcap cap_net_bind_service=+ep /azure-functions-host/Microsoft.Azure.WebJobs.Script.WebHost \
    && apt-get purge -y libcap2-bin && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/*

USER appuser
