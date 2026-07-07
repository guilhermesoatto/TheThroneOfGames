# Build stage — SDK completo (glibc) evita incompatibilidades de toolchain/analyzers
# com Alpine; a saída é framework-dependent (UseAppHost=false), então roda sem
# problema sobre o runtime musl da imagem final.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["GameStore.Catalogo.API/GameStore.Catalogo.API.csproj", "GameStore.Catalogo.API/"]
COPY ["GameStore.Catalogo/GameStore.Catalogo.csproj", "GameStore.Catalogo/"]
COPY ["GameStore.Common/GameStore.Common.csproj", "GameStore.Common/"]
COPY ["GameStore.CQRS.Abstractions/GameStore.CQRS.Abstractions.csproj", "GameStore.CQRS.Abstractions/"]

# Restore dependencies
RUN dotnet restore "GameStore.Catalogo.API/GameStore.Catalogo.API.csproj"

# Copy all source code
COPY . .

# Publish (inclui o build — evita uma segunda compilação redundante)
RUN dotnet publish "GameStore.Catalogo.API/GameStore.Catalogo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage — Alpine (~25MB base vs ~110MB Debian): menor superfície de
# ataque e pull mais rápido em pods escalados pelo HPA.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

# icu-libs: Alpine não traz ICU por padrão — sem isso, formatação de datas/moeda
# cai em modo invariant e muda comportamento silenciosamente.
#
# libcap (setcap): non-root não pode fazer bind em portas <1024 (Kestrel escuta na porta 80,
# ver Program.cs). Conceder a capability via securityContext do Kubernetes (cap_add) sozinho
# NÃO basta para um processo non-root — ela fica só no bounding set, nunca chega ao effective
# set (confirmado em teste real: CapEff/CapAmb permanecem zerados mesmo com CapBnd setado).
# setcap grava a capability diretamente nos xattrs do binário do dotnet, que o kernel aplica
# no Effective set no exec — mecanismo padrão para non-root abrir porta privilegiada.
RUN apk add --no-cache icu-libs libcap \
    && addgroup -S appgroup \
    && adduser -S appuser -G appgroup \
    && setcap cap_net_bind_service=+ep "$(readlink -f "$(which dotnet)")" \
    && apk del libcap

COPY --from=build --chown=appuser:appgroup /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80

USER appuser

# HEALTHCHECK via wget (busybox, já presente na imagem Alpine — sem instalar curl).
# Em produção (Kubernetes) o probe real é o httpGet nativo definido nos manifests
# em k8s/deployments/, que não depende de nenhum binário dentro da imagem.
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
    CMD wget -q -O- http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "GameStore.Catalogo.API.dll"]
