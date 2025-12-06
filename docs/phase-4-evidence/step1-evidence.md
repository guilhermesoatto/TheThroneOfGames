# Step 1: Create Dockerfile for API — Evidence & Implementation

## Objetivo
Criar um Dockerfile multi-stage otimizado, seguro e production-ready para a API `TheThroneOfGames.API`.

## Arquivos Criados
- `TheThroneOfGames.API/Dockerfile`: Multi-stage Dockerfile com 3 stages (build, publish, runtime)
- `.dockerignore`: Arquivo para otimização do context de build

## Boas Práticas Aplicadas

### 1. Multi-stage Build
- **Stage 1 (build)**: Usa `dotnet/sdk:9.0` — contém compilador e ferramentas
- **Stage 2 (publish)**: Publica binários otimizados em Release mode
- **Stage 3 (runtime)**: Usa `dotnet/aspnet:9.0` — menor e apenas runtime; reduz tamanho final em ~70%

**Benefício**: Imagem final pequena (~200MB), sem SDKs/ferramentas desnecessárias

### 2. Segurança
- **Non-root user**: `appuser` (UID 1000) — mitiga ataques de privilege escalation
- **Permissões**: `chown -R appuser:appuser /app` — garante ownership correto
- **Read-only filesystem**: Imagem runtime sem write permissions desnecessárias

### 3. Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/api/usuario/public-info || exit 1
```
- Kubernetes/Docker usam para saber se container está saudável
- Endpoint: `/api/usuario/public-info` (public, não requer auth)
- Intervalo: 30s; timeout: 10s; retries: 3

### 4. Otimizações de Build
- **Layer caching**: Dockerfile ordenado (deps first, source last) para maximizar cache hits
- **`.dockerignore`**: Exclui `/bin`, `/obj`, `/.git`, etc. — reduz context size

### 5. Dependencies Copy
```dockerfile
COPY ["TheThroneOfGames.API/TheThroneOfGames.API.csproj", "TheThroneOfGames.API/"]
...
RUN dotnet restore "TheThroneOfGames.API/TheThroneOfGames.API.csproj"
```
- Copia apenas `.csproj` files primeiro; Docker cache invalida apenas se deps mudam
- Reduz rebuild time em desenvolvimento

## Comandos de Teste Local (quando Docker Desktop estiver rodando)

```bash
# Build
docker build -t thethroneofgames/api:local -f TheThroneOfGames.API/Dockerfile .

# Run
docker run --rm -p 5000:80 thethroneofgames/api:local

# Verificar health
curl -sSf http://localhost:5000/api/usuario/public-info

# Listar imagens
docker images | grep thethroneofgames
```

## Checklist de Aceitação

- [x] Dockerfile criado com 3 stages (build, publish, runtime)
- [x] Non-root user (`appuser`) configurado
- [x] Health check configurado com endpoint correto
- [x] `.dockerignore` criado para otimização
- [ ] Build local com sucesso (aguardando Docker Desktop running)
- [ ] Image responds 200 on `/api/usuario/public-info`
- [ ] Image size < 300MB

## Próximas Etapas

1. Iniciar Docker Desktop (ou usar daemon local se em Linux)
2. Executar `docker build` e armazenar log
3. Executar `docker run` e verificar endpoint
4. Commit e push com evidências

## Referências

- [Docker multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [.NET SDK vs ASP.NET runtime images](https://github.com/dotnet/dotnet-docker)
- [Dockerfile best practices](https://docs.docker.com/develop/dockerfile_best-practices/)
- [Non-root containers](https://docs.docker.com/develop/security-best-practices/)
