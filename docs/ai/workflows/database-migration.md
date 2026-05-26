# Workflow: Database Migration — EF Core + PostgreSQL

> Layer 2 — Execute este workflow toda vez que houver alteração no modelo de domínio que impacte persistência.
> Pré-requisito: `dotnet tool install --global dotnet-ef` (EF Core CLI)

---

## Pré-condições

- [ ] O DbContext e as `IEntityTypeConfiguration` já refletem o novo modelo
- [ ] A connection string local aponta para o banco de dev (`loyalty_db` no `docker-compose`)
- [ ] `docker compose up -d postgres` foi executado (banco local ativo)

---

## Fase 1 — Gerar a Migration

```bash
# Executar a partir da raiz da solution
dotnet ef migrations add <NOME_DA_MIGRATION> \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi \
  --output-dir Persistence/Migrations
```

**Convenção de nome obrigatória:** `YYYYMMDD_HHmm_DescricaoBreve`
```bash
# Exemplo correto
dotnet ef migrations add 20260408_1430_AddLoyaltyAccountTable \
  --project src/LoyaltyPoints.Infrastructure \
  --startup-project src/LoyaltyPoints.WebApi \
  --output-dir Persistence/Migrations
```

---

## Fase 2 — Revisar a Migration (HARD STOP)

**PARE antes de aplicar.** Revise o arquivo gerado em `Persistence/Migrations/`:

```csharp
// Verifique obrigatoriamente:
// 1. Schema correto? (deve ser "loyalty", não "dbo" ou "public")
// 2. Colunas snake_case? (loyalty_accounts, não LoyaltyAccounts)
// 3. Tipos corretos? (timestamptz para DateTimeOffset, jsonb para JSONB)
// 4. Índices gerados? (idx_outbox_unprocessed, idx_loyalty_accounts_customer_id)
// 5. Migration reversa (Down) está correta para rollback?
```

**Checklist de revisão:**
- [ ] Nenhuma coluna `nvarchar` — PostgreSQL usa `text` / `varchar(n)`
- [ ] `DateTimeOffset` → `timestamptz` (não `timestamp`)
- [ ] Enums persistidos como `text` (não `integer`)
- [ ] Índice parcial do Outbox presente: `WHERE processed_at IS NULL`
- [ ] Nenhuma operação `DROP TABLE` inesperada

---

## Fase 3 — Aplicar em Desenvolvimento

```bash
# Aplicar no banco local
dotnet ef database update \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi \
  --connection "Host=localhost;Port=5432;Database=loyalty_db;Username=loyalty_user;Password=local_only_dev"
```

**Verificar resultado:**
```bash
# Conectar via psql e confirmar estrutura
psql -h localhost -U loyalty_user -d loyalty_db -c "\dt loyalty.*"
psql -h localhost -U loyalty_user -d loyalty_db -c "\d loyalty.loyalty_accounts"
```

---

## Fase 4 — Gerar Script SQL para Staging/Prod

**NUNCA** rodar `dotnet ef database update` diretamente em staging ou produção.
Sempre gere um script SQL idempotente e aplique via pipeline:

```bash
# Gera script desde a migration anterior até a nova
dotnet ef migrations script <MIGRATION_ANTERIOR> <MIGRATION_NOVA> \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi \
  --idempotent \
  --output migrations/sql/20260408_1430_AddLoyaltyAccountTable.sql
```

O flag `--idempotent` gera `IF NOT EXISTS` em cada passo — seguro para re-execução.

**Commit o script SQL** junto com a migration C# para rastreabilidade:
```bash
git add src/NomeDoProjeto.Infrastructure/Persistence/Migrations/
git add migrations/sql/
git commit -m "db: migration 20260408_1430_AddLoyaltyAccountTable"
```

---

## Fase 5 — Aplicar em Staging/Produção (via Pipeline)

No pipeline CI/CD (GitHub Actions / Azure DevOps):

```yaml
# .github/workflows/deploy.yml (trecho)
- name: Apply database migrations
  run: |
    psql "${{ secrets.POSTGRES_CONNECTION_STRING }}" \
      -f migrations/sql/20260408_1430_AddLoyaltyAccountTable.sql
  env:
    PGPASSWORD: ${{ secrets.POSTGRES_PASSWORD }}
```

**Alternativa via EF Core no startup (apenas para staging automático):**
```csharp
// Program.cs — aplica migrations pendentes no startup
// Use com cuidado: adequado para staging, NÃO para produção de alta carga
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoyaltyDbContext>();
    await db.Database.MigrateAsync();
}
```

---

## Fase 6 — Rollback

EF Core não suporta rollback automático de DDL no PostgreSQL.
Para reverter uma migration aplicada:

```bash
# Reverter para uma migration específica (executa o método Down())
dotnet ef database update <NOME_DA_MIGRATION_ANTERIOR> \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi
```

**Para produção:** Use o script SQL de rollback gerado manualmente na Fase 2 (documentado no PR).

---

## Comandos de Referência Rápida

```bash
# Listar migrations e seus status
dotnet ef migrations list \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi

# Remover a última migration (apenas se NÃO aplicada ao banco)
dotnet ef migrations remove \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi

# Ver o SQL que seria executado (dry-run)
dotnet ef migrations script --idempotent \
  --project src/NomeDoProjeto.Infrastructure \
  --startup-project src/NomeDoProjeto.WebApi
```

---

## Critérios de Conclusão

- [ ] Migration gerada com nome no padrão `YYYYMMDD_HHmm_DescricaoBreve`
- [ ] Revisão manual feita (checklist Fase 2 completo)
- [ ] Aplicada com sucesso no banco local
- [ ] Script SQL idempotente gerado e commitado
- [ ] Testes de integração passando com o novo schema
