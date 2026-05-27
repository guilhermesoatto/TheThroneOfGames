# FASE 2: Plano de Migração de Eventos (BREAKING CHANGE)

**Data:** 21 de Janeiro de 2026  
**Status:** AGUARDANDO APROVAÇÃO  
**Impacto:** BREAKING - Requer atualização de 70+ arquivos

---

## 📋 Objetivo

Remover dependências do monolito legado (`TheThroneOfGames.Domain`, `TheThroneOfGames.Infrastructure`, `TheThroneOfGames.Application`) e migrar eventos compartilhados para `GameStore.Common`.

---

## 🎯 Componentes a Migrar

### 1. Eventos de Domínio
**Origem:** `TheThroneOfGames.Domain/Events/`  
**Destino:** `GameStore.Common/Events/`

```
TheThroneOfGames.Domain/Events/
├── IDomainEvent.cs           → GameStore.Common.Events.IDomainEvent
├── IEventHandler.cs          → GameStore.Common.Events.IEventHandler<T>
├── IEventBus.cs              → GameStore.Common.Events.IEventBus
├── UsuarioAtivadoEvent.cs    → GameStore.Common.Events.UsuarioAtivadoEvent
├── GameCompradoEvent.cs      → GameStore.Common.Events.GameCompradoEvent
├── PedidoFinalizadoEvent.cs  → GameStore.Common.Events.PedidoFinalizadoEvent
└── (outros eventos...)
```

### 2. Implementação EventBus
**Origem:** `TheThroneOfGames.Infrastructure/Events/SimpleEventBus.cs`  
**Destino:** `GameStore.Common/Messaging/SimpleEventBus.cs`

---

## 📊 Análise de Impacto

### Arquivos Afetados: ~70+
- **GameStore.Usuarios**: 15 arquivos
- **GameStore.Catalogo**: 13 arquivos  
- **GameStore.Vendas**: 10 arquivos
- **TheThroneOfGames.API**: 5 arquivos
- **Testes**: 30+ arquivos

### Dependências a Remover
```xml
<!-- GameStore.*.csproj -->
<ProjectReference Include="..\TheThroneOfGames.Domain\..." />
<ProjectReference Include="..\TheThroneOfGames.Infrastructure\..." />
<ProjectReference Include="..\TheThroneOfGames.Application\..." />
```

### Imports a Substituir
```csharp
// ANTES (Legado)
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Events;
using TheThroneOfGames.Application.Interface;

// DEPOIS (Bounded Context)
using GameStore.Common.Events;
using GameStore.Common.Messaging;
using GameStore.Usuarios.Application.Interfaces; // ou Catalogo/Vendas
```

---

## 🔄 Plano de Execução

### Etapa 1: Criar Estrutura em GameStore.Common
- [ ] Criar namespace `GameStore.Common/Events/`
- [ ] Criar namespace `GameStore.Common/Messaging/`
- [ ] Copiar interfaces e implementações
- [ ] Atualizar namespaces internos

### Etapa 2: Atualizar Bounded Contexts (3 projetos)
- [ ] **GameStore.Usuarios**
  - [ ] Atualizar .csproj (remover referências legadas)
  - [ ] Atualizar using statements (~15 arquivos)
  - [ ] Executar testes unitários
  
- [ ] **GameStore.Catalogo**
  - [ ] Atualizar .csproj
  - [ ] Atualizar using statements (~13 arquivos)
  - [ ] Executar testes unitários
  
- [ ] **GameStore.Vendas**
  - [ ] Atualizar .csproj
  - [ ] Atualizar using statements (~10 arquivos)
  - [ ] Executar testes unitários

### Etapa 3: Atualizar APIs (3 APIs)
- [ ] GameStore.Usuarios.API
- [ ] GameStore.Catalogo.API
- [ ] GameStore.Vendas.API

### Etapa 4: Atualizar TheThroneOfGames.API
- [ ] Atualizar Program.cs (event handler registration)
- [ ] Atualizar Admin controllers se necessário

### Etapa 5: Atualizar Testes
- [ ] GameStore.Usuarios.Tests
- [ ] GameStore.Catalogo.Tests
- [ ] GameStore.Vendas.Tests
- [ ] Testes de integração

### Etapa 6: Validação Final
- [ ] Build completo da solução
- [ ] Executar todos os testes unitários
- [ ] Executar testes de integração
- [ ] Smoke test local (docker-compose)
- [ ] Deploy dev/staging para validação

### Etapa 7: Remover Projetos Legados
⚠️ **APENAS após validação completa:**
- [ ] Remover `TheThroneOfGames.Domain` da solução
- [ ] Remover `TheThroneOfGames.Application` da solução
- [ ] Limpar `TheThroneOfGames.Infrastructure` (manter apenas se necessário para MainDbContext)
- [ ] Atualizar Dockerfiles
- [ ] Atualizar CI/CD pipeline

---

## ⏱️ Estimativa de Tempo

| Etapa | Tempo Estimado |
|-------|----------------|
| 1. Criar estrutura GameStore.Common | 30 min |
| 2. Atualizar bounded contexts | 2h |
| 3. Atualizar APIs | 30 min |
| 4. Atualizar TheThroneOfGames.API | 30 min |
| 5. Atualizar testes | 1h |
| 6. Validação completa | 1h |
| 7. Remover projetos legados | 30 min |
| **TOTAL** | **~6 horas** |

---

## 🚨 Riscos e Mitigações

### Risco 1: Build quebrado durante migração
**Mitigação:** Trabalhar em feature branch, commits incrementais

### Risco 2: Testes falhando após migração
**Mitigação:** Executar testes após cada etapa, não prosseguir se falhar

### Risco 3: Referências circulares
**Mitigação:** GameStore.Common não deve referenciar bounded contexts

### Risco 4: Event handlers não registrados
**Mitigação:** Validar registration no Program.cs após mudanças

---

## ✅ Critérios de Aceitação

- [ ] Solução compila sem erros
- [ ] Todos os testes unitários passam (100%)
- [ ] Testes de integração passam
- [ ] Smoke test local funciona
- [ ] Nenhuma referência a `TheThroneOfGames.Domain/Application/Infrastructure` em bounded contexts
- [ ] CI/CD pipeline executa com sucesso
- [ ] Deploy dev/staging validado

---

## 📝 Notas Importantes

1. **Preservar TheThroneOfGames.API:** Manter como API Gateway/agregador
2. **Manter MainDbContext:** Se for shared DbContext, avaliar migração futura
3. **Backward Compatibility:** Considerar versionamento de eventos se necessário
4. **Documentação:** Atualizar README.md e diagramas de arquitetura

---

## ❓ Aprovação Necessária

**Perguntas para o usuário:**
1. Prosseguir com migração completa agora?
2. Fazer em etapas (1 bounded context por vez)?
3. Adiar até após deploy/validação atual?

**Comando para aprovar:** 
```
"Aprovado - executar FASE 2"
```

**Comando para adiar:**
```
"Adiar FASE 2 - prosseguir com deploy"
```
