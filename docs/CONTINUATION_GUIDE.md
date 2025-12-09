# TheThroneOfGames - Architectural Refactoring: CONTINUATION GUIDE

**Document Date**: November 25, 2025  
**Project Branch**: `readme-and-fixes`  
**Current Phase**: Phase 2 - Complete ✅  
**Overall Status**: Ready for Phase 3

---

## 🎯 PROJECT OBJECTIVE

Transform the TheThroneOfGames monolithic application into an event-driven, independently deployable system with bounded contexts following Domain-Driven Design (DDD) principles. The goal is to establish a foundation that can evolve into a microservices architecture while maintaining loose coupling and enabling eventual asynchronous communication.

### Vision
```
Monolith → Bounded Contexts → Event-Driven System → Microservices
```

---

## 📊 PROJECT PHASES OVERVIEW

### Phase 0: Bounded Contexts Foundation ✅ COMPLETE
**Status**: Delivered and validated  
**Tests**: 42 passing | Build: 38.4s

**What Was Built:**
- 3 bounded contexts: GameStore.Usuarios, GameStore.Catalogo, GameStore.Vendas
- Local repository interfaces (IUsuarioRepository) for internal contracts
- Project structure supporting independent context development
- First test project with entity testing patterns

**Key Files Created:**
- `GameStore.Usuarios/` - User management context
- `GameStore.Catalogo/` - Game catalog context
- `GameStore.Vendas/` - Sales/orders context
- `GameStore.Usuarios.Tests/` - Initial test infrastructure

**Architectural Pattern**: Repository pattern with local interfaces

---

### Phase 1: DTOs & Mappers Implementation ✅ COMPLETE
**Status**: Delivered and validated  
**Tests**: 65 passing | Build: 28.2s

**What Was Built:**

#### DTOs (9 total)
- **UsuarioDTO** - `GameStore.Usuarios/Application/DTOs/`
  - Properties: Id, Name, Email, Role, IsActive, CreatedAt, UpdatedAt
  
- **GameDTO** & **JogoDTO** - `GameStore.Catalogo/Application/DTOs/`
  - GameDTO: Id, Name, Genre, Price, IsAvailable
  - JogoDTO: Alternative representation for context
  
- **PurchaseDTO**, **PedidoDTO**, **ItemPedidoDTO** - `GameStore.Vendas/Application/DTOs/`
  - PurchaseDTO: Id, UserId, GameId, TotalPrice, PurchaseDate, Status
  - PedidoDTO, ItemPedidoDTO: Line item representations

#### Mappers (3 total)
- **UsuarioMapper.cs** - Bidirectional Usuario ↔ UsuarioDTO
  - Methods: ToDTO(), FromDTO(), ToDTOList()
  
- **GameMapper.cs** - Multiple representations
  - Methods: ToDTO(), FromDTO(), ToJogoDTO(), FromJogoDTO(), list variants
  
- **PurchaseMapper.cs** - Purchase entity conversions
  - Methods: ToPurchaseDTO(), FromDTO(), ToPedidoDTO(), list methods

#### Test Projects Added
- `GameStore.Catalogo.Tests/` - 5 mapper tests
- `GameStore.Vendas.Tests/` - 5 mapper tests
- Expanded `GameStore.Usuarios.Tests/` - 7 mapper + entity tests

**Architectural Pattern**: Mapper pattern with null-safe bidirectional conversions

---

### Phase 2: Event-Driven Architecture ✅ COMPLETE
**Status**: Delivered and validated  
**Tests**: 72 passing | Build: 4.8s

**What Was Built:**

#### Event Infrastructure (4 files)
1. **IDomainEvent.cs** - `TheThroneOfGames.Domain/Events/`
   ```csharp
   public interface IDomainEvent
   {
       Guid EventId { get; }
       DateTime OccurredAt { get; }
       string EventName { get; }
   }
   ```

2. **IEventHandler<TEvent>.cs** - Generic handler interface
   ```csharp
   public interface IEventHandler<TEvent> where TEvent : IDomainEvent
   {
       Task HandleAsync(TEvent domainEvent);
   }
   ```

3. **IEventBus.cs** - Event bus contract
   ```csharp
   public interface IEventBus
   {
       void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;
       void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;
       Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
       int GetHandlerCount<TEvent>() where TEvent : IDomainEvent;
   }
   ```

4. **SimpleEventBus.cs** - `TheThroneOfGames.Infrastructure/Events/`
   - Thread-safe, in-memory implementation
   - Uses: Dictionary<Type, List<Delegate>> with lock synchronization
   - Features: Sequential handler execution, no parallelization

#### Domain Events (4 immutable records)
All located in `TheThroneOfGames.Domain/Events/` to avoid circular dependencies:

1. **UsuarioAtivadoEvent**
   - Properties: UsuarioId (Guid), Email (string), Nome (string)
   - Triggered by: UsuarioService.ActivateUserAsync()
   - Published to: Catalogo context handlers

2. **UsuarioPerfillAtualizadoEvent**
   - Properties: UsuarioId (Guid), NovoNome (string), NovoEmail (string)
   - Triggered by: UsuarioService.UpdateUserProfileAsync()
   - Published to: Any interested subscribers

3. **GameCompradoEvent**
   - Properties: GameId (Guid), UserId (Guid), Preco (decimal), NomeJogo (string)
   - Triggered by: GameService.BuyGame()
   - Published to: Usuarios context handlers

4. **PedidoFinalizadoEvent**
   - Properties: PedidoId (Guid), UserId (Guid), TotalPrice (decimal), ItemCount (int)
   - Triggered by: PedidoService.AddPurchaseAsync()
   - Published to: Vendas context handlers

#### Service Integration
- **UsuarioService.cs** - Modified to publish events
  - ActivateUserAsync() → publishes UsuarioAtivadoEvent
  - UpdateUserProfileAsync() → publishes UsuarioPerfillAtualizadoEvent
  
- **GameService.cs** - Modified to publish events
  - BuyGame() → publishes GameCompradoEvent
  
- **PedidoService.cs** - Modified to publish events
  - AddPurchaseAsync() → publishes PedidoFinalizadoEvent

#### Event Handlers (3 cross-context subscribers)
1. **UsuarioAtivadoEventHandler** - `GameStore.Catalogo/Application/EventHandlers/`
   - Reacts to user activation events
   - Synchronizes user info in Catalogo context
   
2. **GameCompradoEventHandler** - `GameStore.Usuarios/Application/EventHandlers/`
   - Reacts to game purchase events
   - Updates user purchase history in Usuarios context
   
3. **PedidoFinalizadoEventHandler** - `GameStore.Vendas/Application/EventHandlers/`
   - Reacts to order completion events
   - Records transaction in Vendas context

#### DI Registration
Modified `TheThroneOfGames.API/Program.cs`:
```csharp
// Create and register event bus
var eventBus = new SimpleEventBus();
builder.Services.AddSingleton<IEventBus>(eventBus);

// Subscribe handlers to enable cross-context communication
eventBus.Subscribe<UsuarioAtivadoEvent>(new UsuarioAtivadoEventHandler());
eventBus.Subscribe<GameCompradoEvent>(new GameCompradoEventHandler());
eventBus.Subscribe<PedidoFinalizadoEvent>(new PedidoFinalizadoEventHandler());
```

#### Test Coverage
- **EventBusTests.cs** - 7 comprehensive tests
  - ✅ PublishAsync calls registered handlers
  - ✅ Multiple handlers execution
  - ✅ Handler count tracking
  - ✅ Unsubscribe functionality
  - ✅ Publication with no handlers (no-op)
  - ✅ Null handler validation
  - ✅ Null event validation

**Architectural Pattern**: Observer pattern + Domain Event Pattern + Event Bus pattern

---

## 📁 PROJECT STRUCTURE

```
TheThroneOfGames/
├── TheThroneOfGames.Domain/
│   ├── Entities/
│   │   ├── Usuario.cs (shared monolith entity)
│   │   ├── GameEntity.cs
│   │   └── Purchase.cs
│   ├── Interfaces/
│   │   └── IUsuarioRepository.cs (shared)
│   └── Events/ ✨ NEW PHASE 2
│       ├── IDomainEvent.cs
│       ├── IEventHandler.cs
│       ├── IEventBus.cs
│       ├── UsuarioAtivadoEvent.cs
│       ├── UsuarioPerfillAtualizadoEvent.cs
│       ├── GameCompradoEvent.cs
│       └── PedidoFinalizadoEvent.cs
│
├── TheThroneOfGames.Infrastructure/
│   ├── Persistence/
│   ├── Repository/
│   ├── ExternalServices/
│   └── Events/ ✨ NEW PHASE 2
│       └── SimpleEventBus.cs
│
├── TheThroneOfGames.Application/
│   ├── UsuarioService.cs (modified Phase 2)
│   ├── GameService.cs (modified Phase 2)
│   └── ... other services
│
├── TheThroneOfGames.API/
│   ├── Program.cs (modified Phase 2)
│   └── ... controllers
│
├── GameStore.Usuarios/ ✨ Phase 0
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── Events/ (local - now uses shared domain)
│   │   └── Interfaces/
│   ├── Application/
│   │   ├── DTOs/ (UsuarioDTO.cs) ✨ Phase 1
│   │   ├── Mappers/ (UsuarioMapper.cs) ✨ Phase 1
│   │   ├── EventHandlers/ (GameCompradoEventHandler.cs) ✨ Phase 2
│   │   └── Services/
│   ├── Infrastructure/
│   │   └── Repository/
│   └── GameStore.Usuarios.csproj
│
├── GameStore.Catalogo/ ✨ Phase 0
│   ├── Domain/
│   ├── Application/
│   │   ├── DTOs/ (GameDTO.cs, JogoDTO.cs) ✨ Phase 1
│   │   ├── Mappers/ (GameMapper.cs) ✨ Phase 1
│   │   ├── EventHandlers/ (UsuarioAtivadoEventHandler.cs) ✨ Phase 2
│   │   └── Services/
│   ├── Infrastructure/
│   └── GameStore.Catalogo.csproj
│
├── GameStore.Vendas/ ✨ Phase 0
│   ├── Domain/
│   ├── Application/
│   │   ├── DTOs/ (PurchaseDTO.cs, PedidoDTO.cs) ✨ Phase 1
│   │   ├── Mappers/ (PurchaseMapper.cs) ✨ Phase 1
│   │   ├── Services/ (PedidoService.cs - modified Phase 2)
│   │   └── EventHandlers/ (PedidoFinalizadoEventHandler.cs) ✨ Phase 2
│   ├── Infrastructure/
│   └── GameStore.Vendas.csproj
│
├── Test/ (Original 40 tests) ✅
│
├── GameStore.Usuarios.Tests/ ✨ Phase 0 → Phase 2
│   ├── UsuarioTests.cs
│   └── EventBusTests.cs (7 new tests) ✨ Phase 2
│
├── GameStore.Catalogo.Tests/ ✨ Phase 1
│   └── GameMapperTests.cs
│
├── GameStore.Vendas.Tests/ ✨ Phase 1
│   └── PurchaseMapperTests.cs
│
├── docs/
│   ├── FINISHING_STEPS.md
│   ├── PHASE_2_EVENT_DRIVEN_ARCHITECTURE.md ✨ NEW
│   ├── ARCHITECTURAL_REFACTORING_SUMMARY.md ✨ NEW
│   └── ... other docs
│
└── TheThroneOfGames.sln
```

---

## 🧪 TEST STATUS

| Project | Test Count | Status | Details |
|---------|-----------|--------|---------|
| GameStore.Usuarios.Tests | 22 | ✅ Pass | 7 EventBusTests + 15 entity/mapper |
| GameStore.Catalogo.Tests | 5 | ✅ Pass | Mapper conversion tests |
| GameStore.Vendas.Tests | 5 | ✅ Pass | Mapper conversion tests |
| Test (Original) | 40 | ✅ Pass | All legacy tests passing |
| **TOTAL** | **72** | **✅ PASS** | **100% Pass Rate** |

**Build Time**: 4.8s  
**Test Execution**: 17.2s

---

## 🔄 EVENT FLOW EXAMPLES

### Example 1: User Activation Flow
```
1. User clicks activation email link
   ↓
2. Controller calls UsuarioService.ActivateUserAsync(token)
   ↓
3. Service finds user by token
   ↓
4. Service calls user.Activate()
   ↓
5. Service publishes UsuarioAtivadoEvent:
   {
     EventId: Guid,
     UsuarioId: user.Id,
     Email: user.Email,
     Nome: user.Name,
     OccurredAt: DateTime.UtcNow
   }
   ↓
6. EventBus distributes event
   ↓
7. UsuarioAtivadoEventHandler (Catalogo) receives event
   ↓
8. Handler executes custom logic (e.g., create user profile in Catalogo)
   ↓
9. Event logged for audit trail
```

### Example 2: Game Purchase Flow
```
1. User clicks "Buy Game" button
   ↓
2. Controller calls GameService.BuyGame(gameId, userId)
   ↓
3. Service finds game, creates Purchase record
   ↓
4. Service publishes GameCompradoEvent:
   {
     EventId: Guid,
     GameId: game.Id,
     UserId: userId,
     Preco: game.Price,
     NomeJogo: game.Name,
     OccurredAt: DateTime.UtcNow
   }
   ↓
5. EventBus distributes event
   ↓
6. GameCompradoEventHandler (Usuarios) receives event
   ↓
7. Handler updates user's purchase history in Usuarios context
   ↓
8. Event recorded for business analytics
```

---

## 💡 KEY ARCHITECTURAL DECISIONS

### 1. Event Location (Shared Domain)
**Decision**: All events stored in `TheThroneOfGames.Domain/Events/`

**Rationale**:
- Avoids circular dependencies between contexts
- Events are core domain concepts, not context-specific implementations
- Simplifies event discovery and understanding
- Single source of truth for event definitions

**Alternative Rejected**: Events in each context would create circular dependencies:
```
GameStore.Usuarios → TheThroneOfGames.Application → GameStore.Vendas
         ↑                                                  ↓
         └──────────────────────────────────────────────────┘
```

### 2. Thread-Safe Event Bus
**Decision**: Lock-based synchronization with Dictionary<Type, List<Delegate>>

**Rationale**:
- Simple, proven pattern
- No external dependencies
- In-memory is suitable for monolith→microservices transition
- Can be replaced with message queue in next phase

**Thread-Safety Implementation**:
```csharp
private object _lockObject = new object();
private Dictionary<Type, List<Delegate>> _handlers = new();

lock (_lockObject)
{
    // Atomic read/write operations
}
```

### 3. Sequential Handler Execution
**Decision**: Execute handlers sequentially, not in parallel

**Rationale**:
- Predictable ordering for audit trails
- Simpler error handling
- Easier to debug issues
- Events recorded in order they occur

**Future Migration**: Can add async queuing in Phase 4

### 4. Generic Handler Interface
**Decision**: `IEventHandler<TEvent>` instead of non-generic base

**Rationale**:
- Type-safe implementations
- Compiler enforces correct event type
- Clear contract per event type
- Reduces mistakes and casting

### 5. Handler Registration in Program.cs
**Decision**: All handlers registered in composition root (API layer)

**Rationale**:
- Central visibility of cross-context communication
- Clear what events are being subscribed to
- Easy to mock for testing
- Simplified handler lifecycle

**Trade-off**: API depends on all contexts, but only for handler types

---

## 🚀 HOW TO CONTINUE FROM HERE

### To Resume Work

1. **Understand Current State**:
   ```bash
   cd c:\Users\Guilherme\source\repos\TheThroneOfGames
   git log --oneline -n 5
   # Should show: 801d1d7 Phase 2: Event-Driven Architecture Implementation - Complete
   ```

2. **Verify All Tests Pass**:
   ```bash
   dotnet test
   # Expected: 72 tests passing
   ```

3. **Review Current Architecture**:
   - Read: `docs/PHASE_2_EVENT_DRIVEN_ARCHITECTURE.md`
   - Read: `docs/ARCHITECTURAL_REFACTORING_SUMMARY.md`

4. **Check Project Structure**:
   ```bash
   # View solution structure
   dotnet sln list
   ```

### Key Files to Understand

**For Event Infrastructure**:
- `TheThroneOfGames.Domain/Events/IDomainEvent.cs`
- `TheThroneOfGames.Domain/Events/IEventBus.cs`
- `TheThroneOfGames.Infrastructure/Events/SimpleEventBus.cs`

**For Service Integration**:
- `TheThroneOfGames.Application/UsuarioService.cs` (lines 60-70 for event publishing)
- `TheThroneOfGames.Application/GameService.cs` (lines 60-70)
- `GameStore.Vendas/Application/Services/PedidoService.cs` (lines 27-37)

**For Handler Patterns**:
- `GameStore.Usuarios/Application/EventHandlers/GameCompradoEventHandler.cs`
- `GameStore.Catalogo/Application/EventHandlers/UsuarioAtivadoEventHandler.cs`

**For DI Setup**:
- `TheThroneOfGames.API/Program.cs` (lines 25-40)

---

## 🎯 NEXT PHASE: PHASE 3 (Future Work)

### Phase 3 Objectives: Command Handling & CQRS Patterns

**When**: After Phase 2 validation is complete

**What to Build**:
1. Command objects for each operation
2. Command validators
3. Command handlers (separate from event handlers)
4. Command query patterns
5. CQRS read models if needed

**Key Files to Create**:
- `GameStore.Usuarios/Application/Commands/`
- `GameStore.Usuarios/Application/Handlers/`
- `GameStore.Catalogo/Application/Commands/`
- Command tests

**Architectural Pattern**: CQRS (Command Query Responsibility Segregation)

### Phase 4 Objectives: Event Sourcing & Persistence

**When**: After Phase 3 completion

**What to Build**:
1. Event store implementation
2. Outbox pattern for reliability
3. Event replay capabilities
4. Snapshots for performance optimization

**Key Files to Create**:
- `TheThroneOfGames.Infrastructure/EventStore/`
- `TheThroneOfGames.Infrastructure/Outbox/`

### Phase 5 Objectives: Async Messaging

**When**: After Phase 4 completion

**Replace**:
- SimpleEventBus → RabbitMQ/Azure Service Bus
- Synchronous → Asynchronous communication
- In-memory → Distributed event handling

### Phase 6 Objectives: Microservices Deployment

**When**: After Phase 5 completion

**Deploy Each Context As**:
- Independent service
- Own database
- Own message queue subscriptions
- Service discovery & load balancing

---

## 📋 COMMON TASKS

### To Add New Domain Event

1. Create event class in `TheThroneOfGames.Domain/Events/NewEvent.cs`:
```csharp
namespace TheThroneOfGames.Domain.Events
{
    public record NewEvent(
        Guid Id,
        string PropertyName
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
}
```

2. Publish from service:
```csharp
var newEvent = new NewEvent(id: value, propertyName: value);
await _eventBus.PublishAsync(newEvent);
```

3. Create handler in appropriate context:
```csharp
public class NewEventHandler : IEventHandler<NewEvent>
{
    public Task HandleAsync(NewEvent domainEvent)
    {
        // Handle event
        return Task.CompletedTask;
    }
}
```

4. Register in `Program.cs`:
```csharp
eventBus.Subscribe<NewEvent>(new NewEventHandler());
```

5. Add tests:
```csharp
[Test]
public async Task EventBus_Publishes_NewEvent()
{
    // Test implementation
}
```

### To Add New Mapper

1. Create DTO in context:
```csharp
public class NewEntityDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

2. Create mapper class:
```csharp
public static class NewEntityMapper
{
    public static NewEntityDTO ToDTO(this NewEntity entity)
    {
        return new NewEntityDTO 
        { 
            Id = entity.Id, 
            Name = entity.Name 
        };
    }
}
```

3. Add tests:
```csharp
[Test]
public void NewEntityMapper_ToDTO_MapsAllProperties()
{
    // Test implementation
}
```

---

## 🔍 DEBUGGING TIPS

### Event Not Published
1. Check service has `_eventBus` injected
2. Verify `await _eventBus.PublishAsync()` is called
3. Check event instantiation has correct parameters
4. Verify handler is subscribed in Program.cs

### Handler Not Called
1. Verify event type matches in subscription
2. Check handler implements `IEventHandler<TEvent>`
3. Verify handler is registered in Program.cs
4. Add Console.WriteLine in handler for debugging

### Circular Dependency Error
1. Check event location (should be in TheThroneOfGames.Domain)
2. Verify contexts don't reference each other directly
3. Check project references in .csproj files
4. Use `dotnet build --no-incremental` to force rebuild

---

## ✅ COMPLETION CHECKLIST FOR PHASE 2

- [x] Event infrastructure created (IDomainEvent, IEventHandler, IEventBus)
- [x] SimpleEventBus implemented with thread-safety
- [x] 4 domain events defined
- [x] UsuarioService integrates event publishing
- [x] GameService integrates event publishing
- [x] PedidoService integrates event publishing
- [x] 3 event handlers created for cross-context communication
- [x] Handlers registered in DI container
- [x] EventBusTests with 7 tests passing
- [x] All 72 tests passing
- [x] Build successful (4.8s)
- [x] Documentation created
- [x] Git commit completed

**Phase 2 Status**: ✅ **COMPLETE AND VALIDATED**

---

## 📞 IMPORTANT CONTACTS & REFERENCES

**Project**:
- Repository: `TheThroneOfGames` (guilhermesoatto)
- Branch: `readme-and-fixes`
- Last Commit: 801d1d7

**Technology Stack**:
- .NET 9.0
- C# with Records for immutability
- NUnit 3.14.0 for testing
- Entity Framework Core
- SQL Server

**Architecture References**:
- Domain-Driven Design (Eric Evans)
- Event Sourcing (Martin Fowler)
- CQRS Pattern (Greg Young)
- Microservices Patterns (Chris Richardson)

---

## 🎊 CONCLUSION

The TheThroneOfGames project has been successfully transformed from a monolithic architecture into an event-driven system with well-defined bounded contexts. Phase 2 establishes a production-ready event infrastructure that can evolve into a full microservices architecture.

**All systems are operational. Ready for Phase 3! 🚀**

**Last Updated**: November 25, 2025  
**Status**: ✅ Ready for Continuation  
**Next Phase**: Phase 3 - Command Handling & CQRS Patterns
