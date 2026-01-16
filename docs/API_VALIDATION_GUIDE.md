# Guia de Validação das APIs no GKE

**Data:** 16 de Janeiro de 2026  
**Cluster:** autopilot-cluster-1 (southamerica-east1)

## 📋 Pré-requisitos

- kubectl configurado com acesso ao cluster GKE
- Cluster e pods rodando (verificar com `kubectl get pods -n thethroneofgames`)

## 🔍 Métodos de Validação

### Método 1: Port-Forward Local (Recomendado para Testes)

Permite acessar as APIs como se estivessem rodando localmente.

#### 1.1 Usuarios API

```bash
# Terminal 1 - Criar túnel
kubectl port-forward svc/usuarios-api 8080:80 -n thethroneofgames

# Terminal 2 - Testar endpoints
curl http://localhost:8080/health

# PowerShell
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
```

**Resposta esperada:**
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-16T03:29:52.8906169Z"
}
```

#### 1.2 Catalogo API

```bash
# Terminal 1
kubectl port-forward svc/catalogo-api 8081:80 -n thethroneofgames

# Terminal 2
curl http://localhost:8081/health
```

#### 1.3 Vendas API

```bash
# Terminal 1
kubectl port-forward svc/vendas-api 8082:80 -n thethroneofgames

# Terminal 2
curl http://localhost:8082/health
```

### Método 2: Acesso Direto via Pod

Executar comandos dentro dos pods.

```bash
# Usuarios API
kubectl exec -it deployment/usuarios-api -n thethroneofgames -- curl http://localhost:80/health

# Catalogo API
kubectl exec -it deployment/catalogo-api -n thethroneofgames -- curl http://localhost:80/health

# Vendas API
kubectl exec -it deployment/vendas-api -n thethroneofgames -- curl http://localhost:80/health
```

### Método 3: Validação Automatizada

Script PowerShell para validar todas as APIs de uma vez:

```powershell
# Salvar como validate-apis.ps1

Write-Host "`n=== Validação de APIs no GKE ===" -ForegroundColor Green

$apis = @(
    @{ Name = "Usuarios"; Service = "usuarios-api"; Port = 8080 },
    @{ Name = "Catalogo"; Service = "catalogo-api"; Port = 8081 },
    @{ Name = "Vendas"; Service = "vendas-api"; Port = 8082 }
)

foreach ($api in $apis) {
    Write-Host "`nTestando $($api.Name) API..." -ForegroundColor Cyan
    
    # Iniciar port-forward em background
    $job = Start-Job -ScriptBlock {
        param($service, $port)
        kubectl port-forward svc/$service ${port}:80 -n thethroneofgames
    } -ArgumentList $api.Service, $api.Port
    
    Start-Sleep -Seconds 3
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($api.Port)/health" -UseBasicParsing -TimeoutSec 5
        Write-Host "✅ $($api.Name) - Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "   Response: $($response.Content)" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ $($api.Name) - Erro: $_" -ForegroundColor Red
    }
    finally {
        Stop-Job $job
        Remove-Job $job
    }
}

Write-Host "`n=== Validação Concluída ===" -ForegroundColor Green
```

Executar:
```powershell
.\validate-apis.ps1
```

## 🔧 Troubleshooting

### Problema: Port-forward não conecta

**Sintoma:**
```
Error from server: error upgrading connection: unable to upgrade connection
```

**Solução:**
```bash
# Verificar se pods estão Running
kubectl get pods -n thethroneofgames

# Verificar logs do pod
kubectl logs deployment/usuarios-api -n thethroneofgames

# Tentar com pod específico ao invés do service
kubectl get pods -n thethroneofgames | grep usuarios
kubectl port-forward pod/usuarios-api-6df7b764c4-gr2bn 8080:80 -n thethroneofgames
```

### Problema: Health check retorna 404

**Sintoma:**
```json
StatusCode: 404
```

**Causas possíveis:**
1. Pod usando imagem antiga (sem endpoint /health)
2. Aplicação não iniciou completamente

**Solução:**
```bash
# Verificar imagem em uso
kubectl get deployment usuarios-api -n thethroneofgames -o yaml | grep "image:"

# Deve mostrar: gcr.io/.../usuarios-api:postgres
# Se mostrar :latest, atualizar deployment

# Verificar logs de startup
kubectl logs deployment/usuarios-api -n thethroneofgames --tail=50
```

### Problema: Connection refused

**Sintoma:**
```
curl: (7) Failed to connect to localhost port 8080: Connection refused
```

**Solução:**
```bash
# Verificar se port-forward está rodando
ps aux | grep "kubectl port-forward"  # Linux/Mac
Get-Process | Where-Object {$_.ProcessName -like "*kubectl*"}  # PowerShell

# Reiniciar port-forward
kubectl port-forward svc/usuarios-api 8080:80 -n thethroneofgames
```

## 📊 Validação de Banco de Dados

### Verificar conectividade PostgreSQL

```bash
# Port-forward do PostgreSQL
kubectl port-forward svc/postgresql-service 5432:5432 -n thethroneofgames

# Conectar com psql (se tiver instalado)
psql -h localhost -p 5432 -U sa -d GameStore

# Listar tabelas
\dt

# Verificar dados
SELECT COUNT(*) FROM "Usuarios";
SELECT COUNT(*) FROM "Jogos";
SELECT COUNT(*) FROM "Pedidos";
```

## 🐰 Validação de RabbitMQ

**Management UI já exposta externamente:**

URL: http://34.39.201.173:15672  
User: guest  
Pass: guest

**Via Port-Forward (alternativa):**
```bash
kubectl port-forward svc/rabbitmq-management 15672:15672 -n thethroneofgames
```

Acessar: http://localhost:15672

## ✅ Checklist de Validação Completa

- [ ] **Pods Running**
  ```bash
  kubectl get pods -n thethroneofgames
  # Todos devem estar 1/1 Running
  ```

- [ ] **Health Checks**
  - [ ] Usuarios API: `/health` retorna 200
  - [ ] Catalogo API: `/health` retorna 200
  - [ ] Vendas API: `/health` retorna 200

- [ ] **Swagger UI** (Development)
  - [ ] Usuarios API: `/swagger` acessível
  - [ ] Catalogo API: `/swagger` acessível
  - [ ] Vendas API: `/swagger` acessível

- [ ] **Banco de Dados**
  - [ ] PostgreSQL conecta (porta 5432)
  - [ ] Tabelas existem (Usuarios, Jogos, Pedidos, etc)

- [ ] **Message Broker**
  - [ ] RabbitMQ Management UI acessível
  - [ ] Connections ativas (verificar na UI)

- [ ] **Logs Limpos**
  ```bash
  kubectl logs deployment/usuarios-api -n thethroneofgames --tail=20
  # Não deve ter erros ou exceptions
  ```

## 🚀 Testes Funcionais Básicos

### 1. Criar Usuário (POST /api/usuario/register)

```bash
kubectl port-forward svc/usuarios-api 8080:80 -n thethroneofgames

curl -X POST http://localhost:8080/api/usuario/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "teste@example.com",
    "password": "Senha@123",
    "nome": "Teste Usuario",
    "role": "User"
  }'
```

### 2. Login (POST /api/usuario/login)

```bash
curl -X POST http://localhost:8080/api/usuario/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "teste@example.com",
    "password": "Senha@123"
  }'
```

**Resposta esperada:** JWT token

### 3. Listar Jogos (GET /api/game)

```bash
kubectl port-forward svc/catalogo-api 8081:80 -n thethroneofgames

curl http://localhost:8081/api/game
```

### 4. Criar Jogo (POST /api/game) - Requer Admin

```bash
curl -X POST http://localhost:8081/api/game \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "Test Game",
    "price": 59.99,
    "genre": "RPG",
    "description": "A test game"
  }'
```

## 📝 Logs e Monitoramento

### Ver logs em tempo real

```bash
# Usuarios API
kubectl logs -f deployment/usuarios-api -n thethroneofgames

# Catalogo API
kubectl logs -f deployment/catalogo-api -n thethroneofgames

# Vendas API
kubectl logs -f deployment/vendas-api -n thethroneofgames

# Todos os pods de uma vez
kubectl logs -f -l app=usuarios-api -n thethroneofgames --all-containers=true
```

### Eventos do cluster

```bash
# Ver eventos recentes
kubectl get events -n thethroneofgames --sort-by='.lastTimestamp' | tail -20

# Filtrar por API específica
kubectl get events -n thethroneofgames --field-selector involvedObject.name=usuarios-api
```

## 🎯 Próximos Passos

Após validação bem-sucedida:

1. **Configurar Ingress** para acesso externo sem port-forward
2. **Implementar SSL/TLS** com certificados Let's Encrypt
3. **Configurar Prometheus** para métricas
4. **Setup Grafana** para dashboards
5. **Implementar CI/CD** para deploys automatizados

---

**Última atualização:** 16 de Janeiro de 2026  
**Status do Cluster:** ✅ Todos os serviços operacionais
