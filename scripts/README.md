# Scripts de Automação - The Throne of Games

Este diretório contém scripts PowerShell para automação de tarefas comuns do projeto.

## Available Scripts

### 1. `run-local.ps1` — Gerenciamento do Ambiente Local (PowerShell)

**Purpose**: Gerenciar ambiente Docker Compose completo com monitoramento.

**Features**:
- Inicia/para/reinicia todos os serviços Docker
- 7 containers: SQL Server, RabbitMQ, Prometheus, Grafana, 3 APIs
- Aguarda healthcheck dos serviços
- Opção para carga automática de dados iniciais
- Exibe URLs de acesso ao final

**Prerequisites**:
- PowerShell 5.1+ (Windows)
- Docker Desktop instalado e em execução
- docker-compose disponível

**Usage**:

```powershell
# Iniciar todos os serviços
.\run-local.ps1

# Iniciar e carregar dados
.\run-local.ps1 -LoadData

# Parar todos os serviços
.\run-local.ps1 -Action stop

# Reiniciar ambiente
.\run-local.ps1 -Action restart

# Ver logs
.\run-local.ps1 -Action logs

# Verificar status
.\run-local.ps1 -Action status
```

**Services**:
- SQL Server (localhost:1433)
- RabbitMQ (localhost:5672, management: localhost:15672)
- Prometheus (localhost:9090)
- Grafana (localhost:3000, admin/admin)
- API Usuários (localhost:5001)
- API Catálogo (localhost:5002)
- API Vendas (localhost:5003)

---

### 2. `load-initial-data.ps1` — Carga de Dados Iniciais (PowerShell)

**Purpose**: Popular banco de dados com dados de teste para desenvolvimento.

**Features**:
- Cria 5 usuários (1 admin + 4 clientes)
- Cria 10 jogos variados
- Cria 10 pedidos de teste
- Ativa usuários automaticamente
- Logs detalhados de progresso

**Prerequisites**:
- PowerShell 5.1+
- Ambiente local rodando (run-local.ps1)
- APIs acessíveis (portas 5001, 5002, 5003)

**Usage**:

```powershell
# Executar carga de dados
.\load-initial-data.ps1
```

**Credentials Created**:
- Admin: admin@thethroneofgames.com / Admin@123
- User1: user1@thethroneofgames.com / User@123
- User2: user2@thethroneofgames.com / User@123
- User3: user3@thethroneofgames.com / User@123
- User4: user4@thethroneofgames.com / User@123

---

### 3. `load-test.ps1` — Teste de Carga Automatizado ⭐ (PowerShell)

**Purpose**: Executar testes de performance com dados aleatórios e 100% de cobertura de endpoints.

**Features**:
- Gera dados aleatórios (usuários, jogos, pedidos)
- Testa TODOS os 8 endpoints das 3 APIs
- Coleta métricas detalhadas: latência, taxa de sucesso, P50/P95/P99
- Testes concorrentes com múltiplas threads
- Gera relatório completo em arquivo
- Métricas individuais por endpoint

**Prerequisites**:
- PowerShell 5.1+
- Ambiente local rodando (run-local.ps1)
- APIs acessíveis e funcionais

**Usage**:

```powershell
# Teste rápido (sanidade - 5/10/10)
.\load-test.ps1 -NumUsuarios 5 -NumJogos 10 -NumPedidos 10 -ConcurrentUsers 2

# Teste padrão (50/100/200)
.\load-test.ps1

# Teste com relatório
.\load-test.ps1 -GenerateReport

# Teste médio (100/200/500)
.\load-test.ps1 -NumUsuarios 100 -NumJogos 200 -NumPedidos 500 -ConcurrentUsers 20 -GenerateReport

# Teste de estresse (500/1000/2000)
.\load-test.ps1 -NumUsuarios 500 -NumJogos 1000 -NumPedidos 2000 -ConcurrentUsers 50 -GenerateReport

# Teste apenas leitura (sem criar dados)
.\load-test.ps1 -SkipDataCreation -ConcurrentUsers 20
```

**Parameters**:
| Parameter | Default | Description |
|-----------|---------|-------------|
| `-NumUsuarios` | 50 | Número de usuários a criar |
| `-NumJogos` | 100 | Número de jogos a criar |
| `-NumPedidos` | 200 | Número de pedidos a criar |
| `-ConcurrentUsers` | 10 | Threads concorrentes |
| `-BaseUrlUsuarios` | http://localhost:5001 | URL da API de Usuários |
| `-BaseUrlCatalogo` | http://localhost:5002 | URL da API de Catálogo |
| `-BaseUrlVendas` | http://localhost:5003 | URL da API de Vendas |
| `-SkipDataCreation` | false | Pula criação de dados |
| `-GenerateReport` | false | Gera arquivo de relatório |

**Endpoint Coverage (100%)**:
| API | Method | Endpoint |
|-----|--------|----------|
| Usuarios | POST | /api/Usuario/pre-register |
| Usuarios | POST | /api/Usuario/activate |
| Usuarios | POST | /api/Usuario/login |
| Catalogo | POST | /api/Game |
| Catalogo | GET | /api/Game |
| Catalogo | GET | /api/Game/{id} |
| Vendas | POST | /api/Pedido |
| Vendas | GET | /api/Pedido |

**Metrics Collected**:
- Total requests, success/failure rate
- Response times: min, avg, max
- Percentiles: P50, P95, P99
- Per-endpoint breakdown

**Example Output**:
```
======================================================================
                    RELATORIO DE TESTE DE CARGA
======================================================================

METRICAS GERAIS:
  Total de requisicoes: 1247
  Requisicoes bem-sucedidas: 1198 (96.07%)
  Requisicoes falhadas: 49 (3.93%)

TEMPOS DE RESPOSTA:
  Minimo: 23.45 ms
  Medio: 156.78 ms
  Maximo: 2345.67 ms
  P50 (Mediana): 134.23 ms
  P95: 478.90 ms
  P99: 1234.56 ms

METRICAS POR ENDPOINT:
Endpoint              Total Success Failed Success % Avg (ms) P95 (ms)
--------              ----- ------- ------ ---------- -------- --------
Usuario/PreRegister      50      48      2       96.0   178.45   345.67
Usuario/Activate         50      48      2       96.0   123.89   289.12
Usuario/Login            48      47      1       97.9    89.34   156.78
Game/Create             100      98      2       98.0   234.56   567.89
Game/List                20      20      0      100.0    67.89   123.45
Game/GetById             30      30      0      100.0    56.78   101.23
Pedido/Create           200     192      8       96.0   289.45   789.01
```

**Interpreting Results**:
- **Success Rate**: >95% excellent, 90-95% good, <90% investigate
- **Avg Time**: <200ms optimal, 200-500ms acceptable, >500ms attention needed
- **P95**: <500ms optimal, 500-1000ms acceptable, >1000ms problem
- **P99**: <1000ms optimal, 1000-2000ms acceptable, >2000ms critical

---

### 4. `load-test-example.ps1` — Guia de Uso do Load Test (PowerShell)

**Purpose**: Exibir exemplos de uso e interpretação de resultados do load-test.

**Usage**:

```powershell
.\load-test-example.ps1
```

**Output**: Mostra exemplos de comandos, interpretação de métricas, cobertura de endpoints e próximos passos.

---

### 5. `helm-dryrun.ps1` — Helm Chart Validation (PowerShell)

**Purpose**: Validate the Helm chart locally before deploying to a Kubernetes cluster.

**Features**:
- Auto-installs Helm (if not present) to user-local directory
- Runs `helm lint` to catch syntax/configuration errors
- Renders templates (`helm template`) to verify YAML generation
- Executes `helm install --dry-run --debug` for deployment validation
- Saves diagnostic artifacts to `artifacts/` directory
- Optionally runs `dotnet test` suite (with `-RunTests` flag)

**Prerequisites**:
- PowerShell 5.1+ (Windows) or PowerShell 7+ (cross-platform)
- Docker (required by k3d/kind if testing against local cluster)
- Kubernetes cluster access (optional; if missing, dry-run will fail gracefully)

**Usage**:

```powershell
# Basic validation (lint + template + dry-run)
.\scripts\helm-dryrun.ps1

# With dotnet tests
.\scripts\helm-dryrun.ps1 -RunTests

# Point to specific kubeconfig
$env:KUBECONFIG = "C:\path\to\kubeconfig"; .\scripts\helm-dryrun.ps1
```

**Output**:
- `artifacts/helm-lint.txt` — Linting results
- `artifacts/helm-template.yaml` — Rendered manifests
- `artifacts/helm-dryrun.txt` — Dry-run installation output
- `artifacts/dotnet-*.txt` — (Optional) Build/test logs

**Example Output**:
```
[2025-12-08T21:42:50] [INFO] Helm is available.
[2025-12-08T21:42:50] [INFO] Running helm lint on ...
==> Linting ...
[INFO] Chart.yaml: icon is recommended
1 chart(s) linted, 0 chart(s) failed
```

---

### 2. `smoke-test.sh` — API Health & Smoke Tests (Bash)

**Purpose**: Validate API endpoints after deployment (Linux/macOS/WSL).

**Features**:
- Polls `/health` and `/health/ready` endpoints
- Configurable retries and polling interval
- Simple HTTP status code validation
- Exit codes (0 = success, 1 = failure) for CI integration

**Prerequisites**:
- Bash 4.0+ or POSIX shell
- `curl` command-line tool
- Running TheThroneOfGames API (e.g., Docker, Kubernetes, or local ASP.NET Core)

**Usage**:

```bash
# Default (localhost:5000)
./scripts/smoke-test.sh

# Custom base URL
BASE_URL=http://api.example.com ./scripts/smoke-test.sh

# Custom retries and interval
BASE_URL=http://localhost:5000 RETRIES=60 SLEEP=1 ./scripts/smoke-test.sh

# In CI (GitHub Actions)
chmod +x ./scripts/smoke-test.sh
BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh
```

**Environment Variables**:
| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `http://localhost:5000` | API base URL |
| `RETRIES` | `30` | Number of retry attempts |
| `SLEEP` | `2` | Seconds to wait between retries |

**Example Output**:
```
[smoke-test] BASE_URL=http://localhost:5000 RETRIES=30 SLEEP=2
[smoke-test] Waiting for http://localhost:5000/health -> (1/30)
[smoke-test] OK http://localhost:5000/health
[smoke-test] OK http://localhost:5000/health/ready
[smoke-test] Performing sample request to /
[smoke-test] GET / -> 200
[smoke-test] Completed
```

---

### 3. `smoke-test.ps1` — API Health & Smoke Tests (PowerShell)

**Purpose**: Cross-platform smoke testing using PowerShell (Windows, macOS, Linux).

**Features**:
- Native PowerShell HTTP client (`Invoke-WebRequest`)
- Configurable retries and polling interval
- Graceful error handling (timeouts, network errors)
- Exit codes (0 = success, 1 = failure) for CI integration

**Prerequisites**:
- PowerShell 5.1+ (Windows) or PowerShell 7+ (cross-platform)
- Running TheThroneOfGames API

**Usage**:

```powershell
# Default (localhost:5000)
.\scripts\smoke-test.ps1

# Custom base URL
.\scripts\smoke-test.ps1 -BaseUrl "http://api.example.com"

# Custom retries and interval
.\scripts\smoke-test.ps1 -BaseUrl "http://localhost:5000" -Retries 60 -IntervalSeconds 1

# Environment variable
$env:BASE_URL = "http://localhost:5000"
.\scripts\smoke-test.ps1
```

**Parameters**:
| Parameter | Default | Description |
|-----------|---------|-------------|
| `-BaseUrl` | `$env:BASE_URL` or `http://localhost:5000` | API base URL |
| `-Retries` | `30` | Number of retry attempts |
| `-IntervalSeconds` | `2` | Seconds to wait between retries |

**Example Output**:
```
[smoke-test] BaseUrl=http://localhost:5000 Retries=30 IntervalSeconds=2
[smoke-test] Waiting for http://localhost:5000/health -> error (1/30)
[smoke-test] OK http://localhost:5000/health
[smoke-test] OK http://localhost:5000/health/ready
[smoke-test] Performing sample request to /
[smoke-test] GET / -> 200
[smoke-test] Completed
```

---

## Local Testing Workflow

### Test with Docker Compose

1. **Start API locally**:
```bash
docker-compose up -d
# Wait ~10 seconds for services to be ready
```

2. **Run smoke tests**:
```bash
# Bash (Linux/macOS/WSL)
BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh

# PowerShell (Windows)
.\scripts\smoke-test.ps1 -BaseUrl "http://localhost:5000"
```

3. **View results**:
```bash
docker-compose logs api
```

---

### Test with Kubernetes (k3d)

1. **Create local cluster**:
```powershell
k3d cluster create ttog --agents 2 --api-port 6550 --wait
k3d kubeconfig write ttog
$env:KUBECONFIG = "$env:USERPROFILE\.config\k3d\kubeconfig-ttog.yaml"
```

2. **Deploy with Helm**:
```bash
helm install thethroneofgames helm/thethroneofgames \
  -f helm/thethroneofgames/values-dev.yaml \
  -n thethroneofgames --create-namespace --wait
```

3. **Port-forward and test**:
```bash
kubectl port-forward -n thethroneofgames svc/thethroneofgames-api-service 5000:80 &
sleep 2

BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh

kill %1  # Kill background port-forward
```

4. **Cleanup**:
```powershell
k3d cluster delete ttog
```

---

## CI Integration

### GitHub Actions (Linux Runner)

The `smoke-test.sh` is automatically executed in the `.github/workflows/deploy-kubernetes.yml` workflow after Helm deployment:

```yaml
- name: Run smoke tests
  run: |
    kubectl port-forward -n thethroneofgames svc/thethroneofgames-api-service 5000:80 &
    PF_PID=$!
    sleep 5

    chmod +x ./scripts/smoke-test.sh
    BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh

    kill $PF_PID || true
```

### GitHub Actions (Windows Runner)

For Windows-based CI pipelines, use the PowerShell variant:

```yaml
- name: Run smoke tests (Windows)
  if: runner.os == 'Windows'
  run: |
    kubectl port-forward -n thethroneofgames svc/thethroneofgames-api-service 5000:80 &
    Start-Sleep -Seconds 5

    .\scripts\smoke-test.ps1 -BaseUrl "http://localhost:5000"
```

---

## Troubleshooting

### Script Not Found / Permission Denied

**Bash**:
```bash
chmod +x ./scripts/smoke-test.sh
./scripts/smoke-test.sh
```

**PowerShell**:
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\scripts\smoke-test.ps1
```

### Timeout / API Not Responding

- Verify API is running: `curl http://localhost:5000/` or `Invoke-WebRequest http://localhost:5000/`
- Increase retries: `RETRIES=60 ./scripts/smoke-test.sh`
- Check logs: `docker-compose logs api` or `kubectl logs -n thethroneofgames -l app=api`

### Helm Chart Validation Fails

Check detailed errors:
```bash
# Lint only
helm lint helm/thethroneofgames

# Template rendering
helm template thethroneofgames helm/thethroneofgames -f helm/thethroneofgames/values-dev.yaml

# Dry-run install
helm install --dry-run --debug thethroneofgames helm/thethroneofgames -f helm/thethroneofgames/values-dev.yaml
```

---

## Contribution

When adding new scripts:
1. Follow the naming convention: `<purpose>-<platform>.{sh|ps1}`
2. Include shebang (`#!/bin/bash` or `#!/usr/bin/env pwsh`) for clarity
3. Add comprehensive error handling and logging
4. Update this README with usage examples
5. Test locally before committing

---

## Related Documentation

- [Helm Chart README](../helm/thethroneofgames/README.md)
- [CI/CD Workflows](.github/workflows/)
- [Docker Compose Setup](docker-compose.yml)
- [Kubernetes Manifests](kubernetes/)
