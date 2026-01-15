# MIGRAÇÃO POSTGRESQL - RESUMO

**Data**: 15/01/2026  
**Status**: ✅ Em Progresso (70% completo)

---

## ✅ COMPLETADO

### 1. Configurações Atualizadas
- ✅ Connection strings alteradas (SQL Server → PostgreSQL)
- ✅ appsettings.json dos 3 microservices
- ✅ docker-compose.yml (postgres:16-alpine)
- ✅ k8s/configmaps.yaml (DB_HOST: postgresql-service, DB_PORT: 5432)

### 2. Código Atualizado
- ✅ GameStore.Usuarios.Infrastructure → UseNpgsql
- ✅ GameStore.Catalogo.Infrastructure → UseNpgsql
- ✅ GameStore.Vendas.Infrastructure → UseNpgsql

### 3. Packages NuGet
- ✅ Npgsql.EntityFrameworkCore.PostgreSQL 9.0.0 adicionado em:
  - GameStore.Usuarios
  - GameStore.Catalogo
  - GameStore.Vendas

### 4. Kubernetes
- ✅ StatefulSet PostgreSQL criado (`k8s/statefulsets/postgresql.yaml`)
- ✅ Service postgresql-service configurado
- ✅ PostgreSQL pod RUNNING no GKE (postgresql-0)
- ✅ SQL Server removido do GKE

### 5. Docker Local
- ✅ PostgreSQL 16 Alpine rodando
- ✅ Volume postgresql-data criado

---

## ⏳ PENDENTE (30%)

### 1. Migrations (PRÓXIMO PASSO)
```bash
# Usuarios
dotnet ef migrations add InitialPostgreSQL --project GameStore.Usuarios --startup-project GameStore.Usuarios.API --context UsuariosDbContext
dotnet ef database update --project GameStore.Usuarios --startup-project GameStore.Usuarios.API --context UsuariosDbContext

# Catalogo
dotnet ef migrations add InitialPostgreSQL --project GameStore.Catalogo --startup-project GameStore.Catalogo.API --context CatalogoDbContext
dotnet ef database update --project GameStore.Catalogo --startup-project GameStore.Catalogo.API --context CatalogoDbContext

# Vendas
dotnet ef migrations add InitialPostgreSQL --project GameStore.Vendas --startup-project GameStore.Vendas.API --context VendasDbContext
dotnet ef database update --project GameStore.Vendas --startup-project GameStore.Vendas.API --context VendasDbContext
```

### 2. Rebuild Imagens Docker
```bash
# Local (testar primeiro)
docker-compose build usuarios-api catalogo-api vendas-api
docker-compose up -d

# GCR (depois do teste local)
docker build -t gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres -f GameStore.Usuarios.API/Dockerfile .
docker build -t gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres -f GameStore.Catalogo.API/Dockerfile .
docker build -t gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres -f GameStore.Vendas.API/Dockerfile .

docker push gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres
docker push gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres
docker push gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres
```

### 3. Atualizar Deployments GKE
```bash
# Atualizar imagens nos deployments
kubectl set image deployment/usuarios-api usuarios-api=gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres -n thethroneofgames
kubectl set image deployment/catalogo-api catalogo-api=gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres -n thethroneofgames
kubectl set image deployment/vendas-api vendas-api=gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres -n thethroneofgames

# Aguardar rollout
kubectl rollout status deployment/usuarios-api -n thethroneofgames
kubectl rollout status deployment/catalogo-api -n thethroneofgames
kubectl rollout status deployment/vendas-api -n thethroneofgames
```

### 4. Validação Final
- [ ] Testar autenticação (POST /api/Usuario/login)
- [ ] Testar CRUD jogos
- [ ] Verificar eventos RabbitMQ
- [ ] Validar HPA funcionando
- [ ] Executar testes de integração

---

## 📊 COMPARATIVO: SQL Server vs PostgreSQL

| Aspecto | SQL Server | PostgreSQL | Ganho |
|---------|------------|------------|-------|
| **Imagem** | 2GB | 109MB | 🟢 95% menor |
| **RAM Mínima** | 2Gi | 256Mi | 🟢 87% menos |
| **CPU Mínima** | 500m | 250m | 🟢 50% menos |
| **GKE Autopilot** | ❌ Problema com hostPath | ✅ Funciona | 🟢 |
| **Startup** | ~60s | ~5s | 🟢 92% mais rápido |
| **Licença** | Proprietário | Open Source | 🟢 |
| **Custo Mensal** | ~$50+ (Cloud SQL) | ~$7-15 (Cloud SQL) | 🟢 70% economia |

---

## 🎯 VANTAGENS DA MIGRAÇÃO

### Técnicas
1. ✅ **Compatível com GKE Autopilot** - Sem problemas de volume
2. ✅ **Menos recursos** - APIs podem rodar com menos memória
3. ✅ **Startup rápido** - 5s vs 60s
4. ✅ **Imagem leve** - Downloads mais rápidos
5. ✅ **EF Core suporte nativo** - Sem problemas de compatibilidade

### Operacionais
1. ✅ **Custo reduzido** - 70% economia no banco gerenciado
2. ✅ **Manutenção simples** - Menos complexidade
3. ✅ **Alta disponibilidade** - Replicação nativa do PostgreSQL
4. ✅ **Backup facilitado** - pg_dump integrado
5. ✅ **Monitoramento** - Ferramentas open source abundantes

---

## 📝 ARQUIVOS MODIFICADOS

### Código (.cs)
1. `GameStore.Usuarios/Infrastructure/Extensions/UsuariosInfrastructureExtensions.cs`
2. `GameStore.Catalogo/Infrastructure/Extensions/CatalogoInfrastructureExtensions.cs`
3. `GameStore.Vendas/Infrastructure/Extensions/VendasInfrastructureExtensions.cs`

### Projetos (.csproj)
1. `GameStore.Usuarios/GameStore.Usuarios.csproj`
2. `GameStore.Catalogo/GameStore.Catalogo.csproj`
3. `GameStore.Vendas/GameStore.Vendas.csproj`

### Configurações
1. `GameStore.Usuarios.API/appsettings.json`
2. `GameStore.Catalogo.API/appsettings.json`
3. `GameStore.Vendas.API/appsettings.json`
4. `docker-compose.yml`
5. `k8s/configmaps.yaml`

### Kubernetes
1. `k8s/statefulsets/postgresql.yaml` (NOVO)
2. `k8s/statefulsets/sqlserver.yaml` (REMOVIDO do GKE)

---

## 🚀 PRÓXIMOS COMANDOS

```bash
# 1. Criar e aplicar migrations
cd GameStore.Usuarios.API
dotnet ef database update

cd ../GameStore.Catalogo.API
dotnet ef database update

cd ../GameStore.Vendas.API
dotnet ef database update

# 2. Testar localmente
docker-compose up -d
curl http://localhost:5001/swagger

# 3. Deploy no GKE
docker build e push (3 APIs)
kubectl set image (3 deployments)
kubectl get pods -n thethroneofgames -w

# 4. Validar
kubectl port-forward svc/usuarios-api 5001:5001 -n thethroneofgames
curl http://localhost:5001/swagger
```

---

## ✅ STATUS ATUAL NO GKE

```
PostgreSQL: ✅ RUNNING (postgresql-0)
RabbitMQ:   ✅ RUNNING (rabbitmq-0)
APIs:       ⏳ CrashLoopBackOff (imagens antigas com SQL Server)
```

**Ação necessária**: Rebuild e redeploy das 3 APIs com código PostgreSQL

---

**Conclusão**: Migração 70% completa. PostgreSQL rodando. Falta aplicar migrations e redesenhar APIs.
