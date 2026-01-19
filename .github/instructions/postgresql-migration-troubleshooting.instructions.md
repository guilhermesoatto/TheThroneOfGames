---
applyTo: '**'
---

# PostgreSQL Migration - Troubleshooting & Lessons Learned

## Data da Migração: 15 de Janeiro de 2026

## Contexto
Migração de SQL Server 2019 para PostgreSQL 16 Alpine para compatibilidade com GKE Autopilot e otimização de recursos.

## Problemas Encontrados e Soluções

### 1. Incompatibilidade de Versão do EF Core Design

**Problema:**
```bash
error: NU1202: O pacote Microsoft.EntityFrameworkCore.Design 10.0.2 não é 
compatível com net9.0 (.NETCoreApp,Version=v9.0)
```

**Causa Raiz:**
NuGet resolveu automaticamente para a versão mais recente (10.0.2) que requer .NET 10, mas o projeto usa .NET 9.

**Solução:**
```bash
# Especificar versão explicitamente
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
```

**Lição Aprendida:**
✅ Sempre especificar versões de pacotes explicitamente em ambientes multi-target
✅ Verificar compatibilidade de target framework antes de adicionar pacotes

---

### 2. Conflito de Migrations Existentes

**Problema:**
```
System.NullReferenceException: Object reference not set to an instance of an object
at Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationsModelDiffer.Initialize
```

**Causa Raiz:**
Migrations antigas do SQL Server causavam conflito ao tentar criar novas migrations para PostgreSQL.

**Solução:**
```bash
# Remover migrations antigas
Remove-Item GameStore.Usuarios/Infrastructure/Migrations/*.cs -Force
Remove-Item GameStore.Catalogo/Infrastructure/Migrations/*.cs -Force
Remove-Item GameStore.Vendas/Infrastructure/Migrations/*.cs -Force

# Criar novas migrations
dotnet ef migrations add InitialPostgreSQL --project <context> --startup-project <api>
```

**Lição Aprendida:**
✅ Limpar migrations antigas ao trocar providers de banco de dados
✅ NullReferenceException em migrations geralmente indica conflito de modelos

---

### 3. Connection String para Ambiente Local vs Kubernetes

**Problema:**
```
Este host não é conhecido: postgresql-service
```

**Causa Raiz:**
Connection string configurada para Kubernetes (postgresql-service) não funciona em ambiente local.

**Solução:**
Criar `appsettings.Development.json` específico:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=GameStore;Username=sa;Password=YourSecurePassword123!"
  }
}
```

**Lição Aprendida:**
✅ Sempre separar configurações de ambiente (Development vs Production)
✅ Usar variáveis de ambiente ou appsettings específicos por ambiente
✅ Connection strings devem ser diferentes para local (localhost) e K8s (service names)

---

### 4. Configuração de Entidade com IsRequired(true) Incorreta

**Problema:**
Campo `ActiveToken` marcado como `.IsRequired()` causava NullReferenceException na migration.

**Solução:**
```csharp
// Antes (causava erro):
entity.Property(u => u.ActiveToken).IsRequired();

// Depois (correto):
entity.Property(u => u.ActiveToken).IsRequired(false).HasMaxLength(255);
```

**Lição Aprendida:**
✅ Campos nullable no domínio devem ter `.IsRequired(false)` no DbContext
✅ Sempre adicionar MaxLength para strings para evitar problemas de performance

---

### 5. Porta do Container Docker (80) vs Porta da Aplicação (5001)

**Problema:**
```
Readiness probe failed: dial tcp 10.62.128.48:5001: connect: connection refused
```

**Causa Raiz:**
Dockerfile configurava ASP.NET para escutar na porta 80, mas deployment Kubernetes configurava ASPNETCORE_URLS=http://+:5001.

**Solução:**
Remover configuração de porta customizada do deployment:
```yaml
# REMOVER esta configuração:
- name: ASPNETCORE_URLS
  value: "http://+:5001"

# Ajustar containerPort para 80 (padrão do Dockerfile)
ports:
- containerPort: 80
```

**Lição Aprendida:**
✅ Deixar o Dockerfile definir a porta padrão (80 para ASP.NET Core)
✅ Evitar sobrescrever ASPNETCORE_URLS via env vars sem necessidade
✅ Health check ports devem corresponder à porta real do container

---

### 6. Swagger Apenas em Development

**Problema:**
```
Readiness probe failed: HTTP probe failed with statuscode: 404
Path: /swagger
```

**Causa Raiz:**
Program.cs configurado para habilitar Swagger apenas em Development:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Soluções Possíveis:**

**Opção 1 - Habilitar Swagger em Production (não recomendado para produção real):**
```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

**Opção 2 - Criar endpoint /health dedicado (RECOMENDADO):**
```csharp
app.MapHealthChecks("/health");
```

**Opção 3 - Usar root path como health check:**
```csharp
app.MapGet("/", () => "OK");
```

**Lição Aprendida:**
✅ Sempre implementar endpoints /health dedicados para Kubernetes probes
✅ Não depender de /swagger para health checks em Production
✅ Health checks devem ser leves e não expor informações sensíveis

---

### 7. Corrupção de Arquivos YAML Durante Edições

**Problema:**
```yaml
# Arquivo corrompido:
port: 5001
initialDelaySeconds: 30
periodSe80  # <-- corrupção
```

**Causa Raiz:**
Múltiplas operações de replace_string_in_file em sequência causaram sobreposição de texto.

**Solução:**
- Remover arquivo corrompido: `Remove-Item <file> -Force`
- Recriar arquivo completo usando here-string do PowerShell
- Ou usar editor manual para corrigir

**Lição Aprendida:**
✅ Validar sintaxe YAML após edições automáticas (`kubectl apply --dry-run=client`)
✅ Para arquivos pequenos, considerar recriar ao invés de múltiplos replaces
✅ Fazer backup antes de edições complexas

---

### 8. Build Docker Cancelado por Múltiplos Processos Paralelos

**Problema:**
```
ERROR: failed to solve: Canceled: context canceled
```

**Causa Raiz:**
Iniciar 3 builds Docker em paralelo com `-isBackground=true` causou conflito de recursos.

**Solução:**
Executar builds sequencialmente:
```bash
docker build -t image1 .
docker build -t image2 .
docker build -t image3 .
```

**Lição Aprendida:**
✅ Docker builds devem ser sequenciais para evitar contenção de I/O
✅ Usar `-isBackground=true` apenas para comandos de monitoramento
✅ Considerar BuildKit para builds paralelos otimizados se necessário

---

### 9. Docker Image Tag Mismatch (CRITICAL)

**Problema:**
```
Liveness probe failed: HTTP probe failed with statuscode: 404
Readiness probe failed: HTTP probe failed with statuscode: 404
```

Catalogo e Vendas APIs ficavam em crash loop infinito (67+ restarts) enquanto Usuarios API funcionava perfeitamente, apesar de código idêntico.

**Causa Raiz:**
Deployment YAML configurado para puxar imagem antiga `:latest` (SQL Server) ao invés da nova imagem `:postgres` (PostgreSQL com endpoint /health).

**Como Identificar:**
```bash
# Verificar imagem atual no cluster
kubectl get deployment catalogo-api -n thethroneofgames -o yaml | grep "image:"
# Output: image: gcr.io/.../catalogo-api:latest  # ❌ ERRADO

# Verificar eventos do pod
kubectl describe pod <pod-name> | grep "Image"
# Pulling image "gcr.io/.../catalogo-api:latest"

# Verificar logs
kubectl logs <pod-name> --previous
# Mostra "Application is shutting down" sem erros
# Indica que app inicia mas algo está faltando (health endpoint)
```

**Solução:**
```yaml
# deployment.yaml - ANTES (errado):
image: gcr.io/project-62120210-43eb-4d93-954/catalogo-api:latest

# deployment.yaml - DEPOIS (correto):
image: gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres
```

Aplicar mudança:
```bash
kubectl apply -f k8s/deployments/catalogo-api.yaml
kubectl apply -f k8s/deployments/vendas-api.yaml
# Kubernetes fará rolling update automaticamente
```

**Lição Aprendida:**
✅ SEMPRE verificar tag da imagem no deployment YAML após rebuild
✅ Usar tags específicas (`:postgres`, `:v1.0.0`) ao invés de `:latest` em produção
✅ Validar que deployment YAML foi atualizado junto com docker build/push
✅ Se pod crashar sem erro nos logs, verificar se imagem está correta
✅ Comparar deployment working vs failing para identificar diferenças

**Sintomas de Tag Incorreto:**
- Pod inicia e entra em "Running" status
- Logs mostram startup normal, depois "Application is shutting down"
- Health checks retornam 404 (endpoint não existe na imagem antiga)
- Restart count aumenta rapidamente (crash loop)
- Deployment idêntico funciona para outro serviço (indica problema específico)

---

## Comandos Úteis para Debug

### Verificar Logs de Pod
```bash
kubectl logs <pod-name> -n thethroneofgames --tail=50
```

### Verificar Health Probes
```bash
kubectl describe pod <pod-name> -n thethroneofgames | Select-String "Liveness|Readiness|Unhealthy"
```

### Testar Endpoint Internamente
```bash
kubectl port-forward svc/<service> 8080:80 -n thethroneofgames
curl http://localhost:8080/health
```

### Verificar Connection String
```bash
kubectl exec -it <pod> -n thethroneofgames -- env | grep -i connection
```

### Aplicar com Validação
```bash
kubectl apply -f deployment.yaml --dry-run=client
```

---

## Checklist de Migração de Banco de Dados

- [ ] Instalar provider NuGet correto (Npgsql.EntityFrameworkCore.PostgreSQL)
- [ ] Instalar EF Core Design com versão compatível
- [ ] Atualizar DbContext para UseNpgsql
- [ ] Criar appsettings.Development.json com localhost
- [ ] Remover migrations antigas
- [ ] Criar novas migrations para novo provider
- [ ] Aplicar migrations em ambiente local
- [ ] Testar conexão local com PostgreSQL
- [ ] Atualizar connection strings no appsettings.json (Kubernetes service names)
- [ ] Atualizar ConfigMaps do Kubernetes
- [ ] Criar/atualizar StatefulSet do banco de dados
- [ ] Rebuild imagens Docker
- [ ] Push para registry
- [ ] Atualizar deployments no cluster
- [ ] Validar pods Running e Ready
- [ ] Executar smoke tests

---

## Benefícios da Migração PostgreSQL

### Recursos
| Métrica | SQL Server 2019 | PostgreSQL 16 Alpine | Melhoria |
|---------|-----------------|----------------------|----------|
| Tamanho da Imagem | 2GB | 109MB | **95% menor** |
| RAM Mínima | 2Gi | 256Mi | **87% menor** |
| CPU Mínima | 500m | 250m | **50% menor** |
| Tempo de Startup | ~60s | ~5s | **92% mais rápido** |

### Custos Estimados (GKE)
- SQL Server: $50-70/mês
- PostgreSQL: $7-15/mês
- **Economia: ~70%**

### Compatibilidade
- ✅ GKE Autopilot: PostgreSQL compatível (PVC)
- ❌ GKE Autopilot: SQL Server incompatível (hostPath)

---

## Estado Atual da Migração

### ✅ Concluído
1. Código migrado para Npgsql
2. Migrations criadas e aplicadas localmente
3. Imagens Docker construídas com PostgreSQL
4. Imagens pushed para GCR (gcr.io/project-62120210-43eb-4d93-954/*:postgres)
5. PostgreSQL StatefulSet rodando em GKE (postgresql-0: 1/1 Running)
6. Deployments atualizados com novas imagens

### ⚠️ Pendente
1. Adicionar endpoint /health nas APIs
2. Corrigir health check paths nos deployments
3. Validar pods Ready (1/1)
4. Testar conectividade end-to-end
5. Executar testes de integração

### 🎯 Próximos Passos Imediatos
1. Implementar endpoints /health nas 3 APIs
2. Atualizar deployments com path correto
3. Aplicar mudanças no cluster
4. Validar deployment completo
