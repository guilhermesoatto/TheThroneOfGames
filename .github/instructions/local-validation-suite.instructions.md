---
applyTo: 'scripts/local-validation/**'
---

# Local Validation Suite - Complete Documentation

**Created**: 15 de Janeiro de 2026  
**Purpose**: Detect configuration errors before commit, reducing pipeline failures  
**Status**: ✅ Operational (with known issues documented)

---

## Executive Summary

Suite de validação local em 7 camadas para executar antes de commit. Identifica problemas de build, testes, containers, orchestration, pipeline e segurança.

**First Execution Results**:
- Total Duration: **61.3s**
- Passed: **2/7** (28.6%)
- Failed: **5/7** (71.4%)
- **Status**: Issues identified, requires fixes

---

## Validation Layers

### Layer 0: Build Validation
**File**: `run-all-validations.ps1` (embedded)  
**Status**: ✅ **PASSED**  
**Duration**: ~4s

Compiles entire solution to ensure no syntax errors.

```powershell
dotnet build TheThroneOfGames.sln --verbosity quiet
```

### Layer 1: Unit Tests
**File**: `01-unit-tests.ps1`  
**Status**: ❌ **FAILED** (3/4 projects)  
**Duration**: 27.2s

**Results**:
- ✅ GameStore.Catalogo.Tests: PASSED (40 tests)
- ✅ GameStore.Usuarios.Tests: PASSED (61 tests)
- ✅ GameStore.Vendas.Tests: PASSED
- ❌ GameStore.Common.Tests: **FAILED** (2 tests)

**Failures**:
1. `RabbitMqConsumer_Constructor_WithInvalidHost_ThrowsException`
   - Expected: Generic `Exception`
   - Actual: `BrokerUnreachableException` (more specific)
   - **Fix**: Update test to expect `BrokerUnreachableException`

2. `RabbitMqConsumer_StartConsuming_WithValidQueue_StartsSuccessfully`
   - Error: Queue 'test.queue' does not exist
   - **Fix**: Create queue in test setup or mock properly

### Layer 2: Integration Tests
**File**: `02-integration-tests.ps1`  
**Status**: ❌ **FAILED**  
**Duration**: 0.7s

**Failure Reason**: PostgreSQL container not running  
**Command to Fix**:
```powershell
docker run -d --name postgresql-test `
  -e POSTGRES_USER=sa `
  -e POSTGRES_PASSWORD=YourSecurePassword123! `
  -e POSTGRES_DB=GameStore `
  -p 5432:5432 postgres:16-alpine
```

**Note**: InMemory database issue still present (see Issue #2 below)

### Layer 3: Container Cycle
**File**: `03-containers-cycle.ps1`  
**Status**: ❌ **FAILED**  
**Duration**: 2.7s

**Failure Reason**: Ports already allocated
- Port 5432 (PostgreSQL): Conflict with existing container
- Port 5672 (RabbitMQ): Conflict with existing container

**Fixes**:
```powershell
# Option 1: Stop existing containers first
docker stop postgresql rabbitmq
docker rm postgresql rabbitmq

# Option 2: Use different ports for test containers
# Modify script to use ports 5433, 5673 instead
```

### Layer 4: Orchestration Validation
**File**: `04-orchestration-tests.ps1`  
**Status**: ❌ **FAILED** (1 issue)  
**Duration**: 9.2s

**Results**:
- ✅ Kubernetes manifests: 15/15 valid
- ✅ Docker Compose local: VALID
- ✅ Docker Compose sonarqube: VALID
- ❌ **docker-compose.yml**: INVALID
- ✅ Dockerfiles: 5/5 valid

**Fix Required**: Validate and fix `docker-compose.yml` syntax

### Layer 5: Pipeline Validation
**File**: `05-pipeline-validation.ps1`  
**Status**: ❌ **FAILED** (1 critical issue)  
**Duration**: 14.2s

**Issues**:
1. **Build Warnings**: 88 warnings (threshold: 30)
   - **Priority**: HIGH - blocks pipeline
   - **Action**: Review and suppress/fix warnings

2. **Migrations**: ⚠️ Folders not found
   - GameStore.Usuarios\Infrastructure\Migrations
   - GameStore.Catalogo\Infrastructure\Migrations
   - GameStore.Vendas\Infrastructure\Migrations
   - **Note**: This is expected if migrations were renamed/moved

3. **Vulnerabilities**: ✅ None found
4. **Git Status**: ⚠️ Uncommitted changes (normal during development)

### Layer 6: Pre-Commit Validation
**File**: `06-pre-commit-validation.ps1`  
**Status**: ✅ **PASSED** (with warnings)  
**Duration**: 3.5s

**Warnings Found**: 10 possible secrets (non-blocking)
- CI/CD workflow file
- Test files with hardcoded passwords (OK in test context)
- appsettings.json files with connection strings

**Note**: Warnings are expected in test/dev environments

---

## Critical Issues Identified

### Issue #1: Unit Test Failures in GameStore.Common.Tests

**Impact**: Medium  
**Priority**: Medium

**Problem**: RabbitMQ tests failing due to:
1. Incorrect exception type expectations
2. Missing queue setup in tests

**Fix**:
```csharp
// GameStore.Common.Tests/RabbitMqConsumerTests.cs
[Test]
public void RabbitMqConsumer_Constructor_WithInvalidHost_ThrowsException()
{
    // Change from:
    Assert.Throws<Exception>(() => new RabbitMqConsumer("invalid-host", ...));
    
    // To:
    Assert.Throws<BrokerUnreachableException>(() => new RabbitMqConsumer("invalid-host", ...));
}
```

**Status**: ❌ Not fixed yet

---

### Issue #2: InMemory Database in Integration Tests (CRITICAL)

**Impact**: HIGH - Blocks 23 integration tests  
**Priority**: CRITICAL

**Problem**: WebApplicationFactory files use InMemoryDatabase instead of PostgreSQL

**Files Affected**:
- `GameStore.Usuarios.API.Tests/UsuariosWebApplicationFactory.cs`
- `GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs`
- `GameStore.Vendas.API.Tests/VendasWebApplicationFactory.cs`

**Current (WRONG)**:
```csharp
services.AddDbContext<UsuariosDbContext>(options => 
    options.UseInMemoryDatabase("TestDb_Usuarios"));
```

**Should Be (CORRECT)**:
```csharp
services.AddDbContext<UsuariosDbContext>(options => {
    var connectionString = "Host=localhost;Port=5432;Database=GameStore_Test;Username=sa;Password=YourSecurePassword123!";
    options.UseNpgsql(connectionString);
});
```

**Why This Matters**:
- ❌ InMemory doesn't validate real database behavior
- ❌ Constraints, triggers, stored procedures not tested
- ❌ Migrations not validated
- ❌ Pipeline expects PostgreSQL connection
- ❌ Creates local vs CI/CD environment discrepancy

**Status**: ❌ Identified but not fixed (requires separate PR)

---

### Issue #3: Container Port Conflicts

**Impact**: Low (test environment only)  
**Priority**: Low

**Problem**: Test containers conflict with existing dev containers

**Solutions**:
1. Use different port mappings for test containers (5433 instead of 5432)
2. Stop dev containers before running tests
3. Create docker-compose.test.yml with isolated network

**Status**: ⚠️ Workaround available (stop existing containers first)

---

### Issue #4: Build Warnings (88 > 30 threshold)

**Impact**: HIGH - Blocks pipeline  
**Priority**: HIGH

**Problem**: 88 build warnings exceed pipeline threshold of 30

**Common Warning Types** (need manual review):
- Nullable reference warnings
- Obsolete API usage
- Unused variables
- Missing XML documentation

**Fix Strategy**:
1. Review warnings: `dotnet build > warnings.txt 2>&1`
2. Suppress false positives in .editorconfig
3. Fix legitimate issues
4. Target: <30 warnings

**Status**: ❌ Requires manual review

---

### Issue #5: docker-compose.yml Invalid

**Impact**: Medium  
**Priority**: Medium

**Problem**: Root `docker-compose.yml` has syntax errors

**Debug Command**:
```powershell
docker-compose -f docker-compose.yml config
```

**Status**: ❌ Requires validation and fix

---

## PowerShell Encoding Issue (SOLVED)

**Problem**: Unicode characters (✓✗╔═══╗) caused parse errors

**Symptoms**:
```
Token '}' inesperado na expressão ou instrução
A cadeia de caracteres não tem o terminador
```

**Solution**: Recreated all scripts with ASCII-only characters
- ✓ → "OK" / "PASSED"
- ✗ → "FAIL" / "FAILED"
- ╔═══╗ → "==="

**Status**: ✅ **RESOLVED**

**Lesson Learned**: PowerShell automation scripts must use ASCII encoding, no Unicode

---

## How to Use

### Full Validation Suite

```powershell
cd c:\Users\Guilherme\source\repos\TheThroneOfGames\scripts\local-validation
powershell -ExecutionPolicy Bypass -File .\run-all-validations.ps1
```

**Expected Output**:
```
=== LOCAL VALIDATION SUITE ===

=== STEP 0: BUILD ===
Build completed

=== STEP 1: UNIT TESTS ===
...

=== FINAL RESULTS ===

Step                  Status Duration
----                  ------ --------
0-Build               PASSED N/A
1-Unit Tests          FAILED 27.2s
2-Integration Tests   FAILED 0.7s
3-Containers Cycle    FAILED 2.7s
4-Orchestration       FAILED 9.2s
5-Pipeline Validation FAILED 14.2s
6-Pre-Commit          PASSED 3.5s

Summary:
  Passed: 2/7
  Failed: 5/7
  Total Duration: 61.3s

VALIDATIONS FAILED - Fix issues before committing
```

### Individual Layer Execution

```powershell
cd scripts/local-validation

# Unit tests only
.\01-unit-tests.ps1

# Integration tests (requires PostgreSQL)
.\02-integration-tests.ps1

# Recreate containers
.\03-containers-cycle.ps1

# Validate K8s/Docker files
.\04-orchestration-tests.ps1

# Pipeline simulation
.\05-pipeline-validation.ps1

# Security scans
.\06-pre-commit-validation.ps1
```

---

## Prerequisites Checklist

Before running validation suite:

- [x] .NET 9 SDK installed
- [x] Docker Desktop running
- [x] PowerShell 5.1+ or PowerShell Core 7+
- [ ] PostgreSQL container created (see Layer 2 fix)
- [ ] RabbitMQ container created (optional for most tests)
- [x] Solution built at least once
- [x] ExecutionPolicy configured (Bypass or RemoteSigned)

---

## Fix Priority List

Based on impact and blocker status:

1. **CRITICAL** - InMemory → PostgreSQL reversion (blocks 23 integration tests)
2. **HIGH** - Reduce build warnings from 88 to <30 (blocks pipeline)
3. **HIGH** - Fix docker-compose.yml syntax (blocks orchestration)
4. **MEDIUM** - Fix GameStore.Common.Tests RabbitMQ tests (2 tests)
5. **LOW** - Container port conflicts (workaround available)

---

## Success Criteria

Validation suite passes when:

```
Step                  Status
----                  ------
0-Build               PASSED
1-Unit Tests          PASSED (4/4 projects)
2-Integration Tests   PASSED (after InMemory fix)
3-Containers Cycle    PASSED
4-Orchestration       PASSED (all manifests valid)
5-Pipeline Validation PASSED (warnings <30)
6-Pre-Commit          PASSED (warnings OK)

Summary:
  Passed: 7/7
  Failed: 0/7

ALL VALIDATIONS PASSED - Ready to commit!
```

---

## Known Limitations

1. **Kubernetes Validation**: Requires kubectl installed locally
2. **Container Recreation**: Will fail if ports in use by other services
3. **Integration Tests**: Require real PostgreSQL (InMemory doesn't work)
4. **RabbitMQ**: Some tests expect live RabbitMQ connection
5. **Execution Time**: Full suite takes ~60s, consider running only changed layers

---

## Maintenance

### Adding New Validation Layer

1. Create `0X-new-layer.ps1` in `scripts/local-validation/`
2. Follow template:
   ```powershell
   $ErrorActionPreference = "Stop"
   Write-Host "Description..." -ForegroundColor Yellow
   
   # Validation logic
   
   if ($success) {
       Write-Host "[OK]" -ForegroundColor Green
       exit 0
   } else {
       Write-Host "[FAIL]" -ForegroundColor Red
       exit 1
   }
   ```
3. Add to `run-all-validations.ps1` $validationScripts array
4. Test individually first, then in suite
5. Document in this file

### Updating Thresholds

Edit validation scripts:
- **Warnings**: Line 10 in `05-pipeline-validation.ps1` - `if ($warnings.Count -gt 30)`
- **Large Files**: Line 67 in `06-pre-commit-validation.ps1` - `Where-Object { $_.Length -gt 10MB }`

---

## Troubleshooting

### "Script execution disabled"
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
```

### "Port already allocated"
```powershell
docker ps -a
docker stop <container-name>
docker rm <container-name>
```

### "PostgreSQL container not running"
```powershell
docker run -d --name postgresql-test -e POSTGRES_USER=sa -e POSTGRES_PASSWORD=YourSecurePassword123! -e POSTGRES_DB=GameStore -p 5432:5432 postgres:16-alpine
```

### "Parse error in PowerShell script"
- Ensure file uses ASCII encoding (no Unicode characters)
- Check for unclosed strings or brackets
- Run: `powershell -NoExecute -File script.ps1` to validate syntax

---

## Integration with Git Workflow

### Pre-Commit Hook (Future Enhancement)

```bash
# .git/hooks/pre-commit
#!/bin/sh
cd scripts/local-validation
powershell.exe -ExecutionPolicy Bypass -File run-all-validations.ps1

if [ $? -ne 0 ]; then
    echo "❌ Validation failed. Commit aborted."
    exit 1
fi
```

### CI/CD Pipeline Integration

```yaml
# .github/workflows/ci-cd.yml
jobs:
  local-validation:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run Local Validation Suite
        run: |
          cd scripts/local-validation
          powershell -ExecutionPolicy Bypass -File run-all-validations.ps1
```

---

## Files Created

| File | Size | Purpose |
|------|------|---------|
| `01-unit-tests.ps1` | 1.2KB | Execute unit tests for all bounded contexts |
| `02-integration-tests.ps1` | 1.8KB | Execute API integration tests with dependency checks |
| `03-containers-cycle.ps1` | 1.5KB | Test container lifecycle (stop/remove/create/health) |
| `04-orchestration-tests.ps1` | 1.7KB | Validate K8s manifests, Docker Compose, Dockerfiles |
| `05-pipeline-validation.ps1` | 1.9KB | Simulate CI/CD pipeline checks |
| `06-pre-commit-validation.ps1` | 2.0KB | Security and quality scans |
| `run-all-validations.ps1` | 2.3KB | Master orchestrator script |

**Total**: 7 files, 12.4KB

---

## Metrics & Performance

**First Execution**:
- Build: ~4s
- Unit Tests: 27.2s (101 tests)
- Integration Tests: 0.7s (failed fast - no container)
- Container Cycle: 2.7s
- Orchestration: 9.2s (15 K8s manifests + Docker files)
- Pipeline: 14.2s (includes build + scans)
- Pre-Commit: 3.5s (security scans)
- **Total**: 61.3s

**Expected After Fixes**:
- Unit Tests: ~30s (104 tests including fixed ones)
- Integration Tests: ~15s (with PostgreSQL, 23 tests)
- **Total**: ~75s

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-15 | Initial creation with 7 validation layers |
| 1.1 | 2026-01-15 | Fixed Unicode encoding issues (ASCII rewrite) |
| 1.2 | 2026-01-15 | First execution completed, results documented |

---

## Next Steps

1. ✅ Create validation scripts (COMPLETED)
2. ✅ Execute first validation run (COMPLETED)
3. ✅ Document results and issues (COMPLETED)
4. ❌ Fix Issue #2 - InMemory database (PENDING - separate task)
5. ❌ Fix Issue #4 - Build warnings (PENDING - requires code review)
6. ❌ Fix Issue #5 - docker-compose.yml (PENDING - quick fix)
7. ❌ Fix Unit test failures (PENDING - 2 tests)
8. ❌ Re-run validation suite after fixes
9. ❌ Commit when 7/7 layers pass

