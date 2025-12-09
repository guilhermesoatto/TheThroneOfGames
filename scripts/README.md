# TheThroneOfGames Helper Scripts

This directory contains utility scripts for validating, testing, and deploying the TheThroneOfGames platform.

## Available Scripts

### 1. `helm-dryrun.ps1` — Helm Chart Validation (PowerShell)

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
