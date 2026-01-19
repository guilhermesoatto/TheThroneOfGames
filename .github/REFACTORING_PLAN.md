# Plano de Refatoração - Clean Architecture & Microservices

**Data:** 08/01/2026  
**Branch:** `refactor/clean-architecture`  
**Objetivo:** Remover estrutura legada e manter apenas bounded contexts preparados para microservices

---

## 📊 Análise de Dependências Legadas

### Projetos Legados (A REMOVER)
- ❌ `TheThroneOfGames.Domain` - Substituído pelos bounded contexts
- ❌ `TheThroneOfGames.Application` - Substituído pelos bounded contexts
- ❌ `TheThroneOfGames.Infrastructure` - Parcialmente substituído (manter apenas eventos compartilhados)
- ❌ `TheThroneOfGames.API` - Substituído por APIs individuais

### Projetos a Manter (Bounded Contexts)
- ✅ `GameStore.Catalogo` + `GameStore.Catalogo.API`
- ✅ `GameStore.Usuarios` + `GameStore.Usuarios.API`
- ✅ `GameStore.Vendas` + `GameStore.Vendas.API`
- ✅ `GameStore.Common` - Código compartilhado
- ✅ `GameStore.CQRS.Abstractions` - Abstrações CQRS

### Dependências Encontradas (70+ arquivos)

#### 1. **TheThroneOfGames.Domain.Events** (CRÍTICO - Manter Compartilhado)
Usado por todos os bounded contexts para eventos cross-context:
- `IEvent`, `IEventBus`
- `UsuarioAtivadoEvent`, `GameCompradoEvent`, `PedidoFinalizadoEvent`
- **Solução:** Mover para `GameStore.Common.Events`

#### 2. **TheThroneOfGames.Infrastructure** (PARCIAL)
Componentes a migrar:
- ✅ `SimpleEventBus` → `GameStore.Common.Messaging`
- ✅ `MainDbContext` → REMOVER (bounded contexts têm seus próprios DbContexts)
- ✅ Migrations antigas → REMOVER
- ✅ `MongoDbContext` → Avaliar necessidade

#### 3. **Controllers Admin** (CRÍTICO - Migrar ou Remover)
```
TheThroneOfGames.API/Controllers/Admin/
├── GameController.cs (usa IGameService legado)
├── PromotionController.cs (usa IPromotionService legado)
└── UserManagementController.cs (já migrado para bounded context ✅)
```

**Decisão:** Migrar GameController e PromotionController para bounded contexts correspondentes.

#### 4. **Testes de Integração** (Test/)
Atualmente testam a API monolítica. Precisam ser reorganizados:
- `AdminGameManagementTests.cs` → `GameStore.Catalogo.Tests`
- `AdminPromotionManagementTests.cs` → `GameStore.Catalogo.Tests` ou `GameStore.Vendas.Tests`
- `AdminUserManagementTests.cs` → `GameStore.Usuarios.Tests` (já existe)
- `AuthorizationTests.cs` → `GameStore.Usuarios.Tests`
- `AuthenticationTests.cs` → `GameStore.Usuarios.Tests`

---

## 🎯 Plano de Execução

### **FASE 1: Preparação e Mapeamento** ✅
- [x] Criar branch separada
- [x] Analisar todas as dependências (70+ arquivos)
- [x] Mapear regras de negócio

### **FASE 2: Migração de Código Compartilhado**
1. **Mover eventos para GameStore.Common**
   ```
   TheThroneOfGames.Domain.Events → GameStore.Common.Events
   ├── IEvent.cs
   ├── IEventBus.cs
   ├── IEventHandler.cs
   └── DomainEvents/ (eventos específicos)
   ```

2. **Mover SimpleEventBus**
   ```
   TheThroneOfGames.Infrastructure.Events → GameStore.Common.Messaging
   └── SimpleEventBus.cs
   ```

3. **Atualizar namespaces** em todos os bounded contexts

### **FASE 3: Migração de Controllers Admin**

#### A. GameController (Admin)
**Origem:** `TheThroneOfGames.API/Controllers/Admin/GameController.cs`  
**Destino:** `GameStore.Catalogo.API/Controllers/Admin/GameController.cs`

**Regras de Negócio a Manter:**
- ✅ CRUD de jogos (Create, Read, Update, Delete)
- ✅ Listagem com paginação
- ✅ Filtros por gênero/disponibilidade
- ✅ Autorização [Authorize(Roles = "Admin")]

**Mudanças:**
```diff
- using TheThroneOfGames.Application.Interface;
- using TheThroneOfGames.Domain.Entities;
+ using GameStore.Catalogo.Application.Commands;
+ using GameStore.Catalogo.Application.Queries;
+ using GameStore.CQRS.Abstractions;
```

#### B. PromotionController (Admin)
**Origem:** `TheThroneOfGames.API/Controllers/Admin/PromotionController.cs`  
**Destino:** `GameStore.Catalogo.API/Controllers/Admin/PromotionController.cs` (promoções relacionadas a jogos)

**Decisão:** Promoções pertencem ao Catálogo (descontos em jogos).

### **FASE 4: Reorganização de Testes**

#### Estrutura Alvo:
```
GameStore.Catalogo.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── JogoTests.cs
│   │   └── ValueObjects/
│   ├── Application/
│   │   ├── CommandHandlers/
│   │   └── QueryHandlers/
│   └── Infrastructure/
│       └── Repositories/
└── Integration/
    ├── AdminGameManagementTests.cs
    ├── AdminPromotionManagementTests.cs
    └── GameCatalogTests.cs

GameStore.Usuarios.Tests/
├── Unit/
│   ├── Domain/
│   │   └── UsuarioTests.cs
│   ├── Application/
│   │   └── Services/
│   └── Infrastructure/
└── Integration/
    ├── AdminUserManagementTests.cs
    ├── AuthenticationTests.cs
    ├── AuthorizationTests.cs
    ├── EmailActivationTests.cs
    └── PasswordValidationTests.cs

GameStore.Vendas.Tests/
├── Unit/
│   └── ...
└── Integration/
    └── ...
```

### **FASE 5: Remoção de Código Legado**

#### Ordem de Remoção:
1. ✅ Remover `TheThroneOfGames.Application` (após migrar services)
2. ✅ Remover `TheThroneOfGames.API` (após migrar controllers)
3. ✅ Remover `TheThroneOfGames.Domain` (após migrar eventos)
4. ✅ Limpar `TheThroneOfGames.Infrastructure` (manter apenas o que for compartilhado)
5. ✅ Remover `Test/` (após reorganizar para bounded contexts)

### **FASE 6: Configuração de Microservices Independentes**

#### Cada microservice deve ter:
```yaml
GameStore.Catalogo.API/
├── Program.cs (configuração independente)
├── appsettings.json
├── Dockerfile
└── Properties/launchSettings.json (porta única)

GameStore.Usuarios.API/
├── Program.cs (configuração independente)
├── appsettings.json
├── Dockerfile
└── Properties/launchSettings.json (porta única)

GameStore.Vendas.API/
├── Program.cs (configuração independente)
├── appsettings.json
├── Dockerfile
└── Properties/launchSettings.json (porta única)
```

#### Comunicação Entre Microservices:
- **Eventos:** Via `GameStore.Common.Messaging.IEventBus`
- **HTTP:** Via HttpClient para chamadas síncronas
- **Fila:** RabbitMQ/Kafka para eventos assíncronos (futuro)

### **FASE 7: Documentação**

#### Criar/Atualizar:
1. **`.github/TROUBLESHOOTING.md`**
   - Problemas resolvidos durante migração
   - IUsuarioService DI conflict
   - Test concurrency issues
   - Database migration issues
   - Cross-context data flow

2. **`.github/ARCHITECTURE.md`**
   - Diagrama de bounded contexts
   - Comunicação entre microservices
   - Event flow diagram

3. **`.github/DEPLOYMENT.md`**
   - Como rodar cada microservice
   - Dependências e ordem de inicialização
   - Docker Compose atualizado

4. **`.github/instructions/`** (consolidar)
   - `bounded-contexts.instructions.md`
   - `microservices.instructions.md`
   - `testing-strategy.instructions.md`

---

## ✅ Critérios de Sucesso

### Cada Microservice DEVE:
1. ✅ Compilar independentemente
2. ✅ Ter seu próprio DbContext e database
3. ✅ Ter testes unitários e integração próprios
4. ✅ Rodar em porta isolada
5. ✅ Comunicar via eventos (assíncrono) ou HTTP (síncrono)
6. ✅ Ter documentação completa (README.md próprio)

### Testes DEVEM:
1. ✅ 100% de testes passando para cada bounded context
2. ✅ Testes isolados (sem dependências cross-context)
3. ✅ Testes de integração testam apenas o microservice correspondente
4. ✅ Testes end-to-end (e2e) testam comunicação entre microservices

### Documentação DEVE:
1. ✅ Troubleshooting guide completo
2. ✅ Architecture decision records (ADR)
3. ✅ Deployment instructions atualizadas
4. ✅ Diagramas de arquitetura atualizados

---

## 📝 Notas Importantes

### Regras de Negócio Críticas:
1. **Usuários:** Pré-registro, ativação por email, roles (User/Admin)
2. **Catálogo:** CRUD de jogos, disponibilidade, estoque
3. **Promoções:** Descontos por tempo limitado, múltiplos jogos
4. **Vendas:** Carrinho, pedidos, histórico de compras

### Eventos Cross-Context:
- `UsuarioAtivadoEvent`: Usuarios → outros contextos
- `GameCompradoEvent`: Catalogo → Vendas
- `PedidoFinalizadoEvent`: Vendas → Catalogo (atualizar estoque)

### Dados Compartilhados (Evitar):
- ❌ Cada bounded context deve ter seus próprios dados
- ❌ Evitar foreign keys cross-database
- ✅ Usar eventos para sincronização eventual

---

## 🚀 Próximos Passos

1. **IMEDIATO:** Mover eventos para GameStore.Common.Events
2. **CURTO PRAZO:** Migrar Admin Controllers para bounded contexts
3. **MÉDIO PRAZO:** Reorganizar testes
4. **LONGO PRAZO:** Remover código legado e validar microservices

---

## 🔄 Status Atual
- **Branch:** `refactor/clean-architecture`
- **Compilação:** ✅ Todos os projetos compilando
- **Testes:** ✅ 48/48 (100%) em master
- **Próximo:** Iniciar FASE 2 - Migração de código compartilhado
