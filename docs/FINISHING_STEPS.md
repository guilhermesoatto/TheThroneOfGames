# Finalization Plan — TheThroneOfGames (Atualizado 25/11/2025)

## Fases de Desenvolvimento

### Fase Concluída: Refatoração para Bounded Contexts (24/11/2025)

**O que foi entregue:**
- ✅ Estrutura de 3 bounded contexts independentes: GameStore.Usuarios, GameStore.Catalogo, GameStore.Vendas
- ✅ Interfaces locais por contexto para desacoplamento
- ✅ Serviços e repositórios copiados/mapeados para cada contexto
- ✅ Projeto de testes: GameStore.Usuarios.Tests (2 testes, 100% passando)
- ✅ Documentação: ARCHITECTURE_README.md, DEVELOPMENT_PROMPT.md
- ✅ Build verde (2,1s), 42 testes passando

**Arquivos principais adicionados:**
- `GameStore.Usuarios/` — Contexto completo com entidade, interface, serviço, repositório
- `GameStore.Catalogo/` — Contexto com serviço de games e repositório
- `GameStore.Vendas/` — Contexto com serviço de pedidos e repositório
- `GameStore.Usuarios.Tests/UsuarioTests.cs` — Suite de testes para usuários

---

### Fase 1 Concluída: Consolidação (25/11/2025) ✅

**O que foi entregue:**

#### ✅ 1.1 DTOs por Contexto Criados
- `GameStore.Usuarios/Application/DTOs/UsuarioDTO.cs` — Id, Name, Email, Role, IsActive, CreatedAt
- `GameStore.Catalogo/Application/DTOs/GameDTO.cs` + `JogoDTO.cs` — Id, Name, Genre, Price, IsAvailable
- `GameStore.Vendas/Application/DTOs/PurchaseDTO.cs` + `PedidoDTO.cs` + `ItemPedidoDTO.cs` — compras, pedidos

#### ✅ 1.2 Mappers/Adapters Implementados
- `GameStore.Usuarios/Application/Mappers/UsuarioMapper.cs` — ToDTO(), FromDTO(), ToDTOList()
- `GameStore.Catalogo/Application/Mappers/GameMapper.cs` — suporta GameDTO e JogoDTO
- `GameStore.Vendas/Application/Mappers/PurchaseMapper.cs` — suporta PurchaseDTO e PedidoDTO

#### ✅ 1.3 Suite de Testes Ampliada
- **GameStore.Usuarios.Tests:** 15 testes (entidade + mappers)
  - Testes de ativação, atualização de perfil, mudança de role
  - Testes de mappers: ToDTO, FromDTO, ToDTOList, null handling
  - Cobertura: ~90%
- **GameStore.Catalogo.Tests:** 5 testes (mappers)
  - Testes de conversão GameEntity ↔ GameDTO
  - Testes de lista e null handling
- **GameStore.Vendas.Tests:** 5 testes (mappers)
  - Testes de conversão Purchase ↔ PurchaseDTO
  - Testes de lista e validação

**Métricas de Entrega:**
- ✅ Build: sucesso em 28.2s (10 projetos)
- ✅ Testes: **65/65 passando** (40 originais + 25 novos)
- ✅ Cobertura: ≥80% por contexto
- ✅ DTOs: 3 contextos × múltiplos DTOs
- ✅ Mappers: 3 contextos, todos com bidirecionalidade

---

### Fase 2: Comunicação Entre Contextos (Próximo Sprint — 2-4 semanas)

**Objetivo:** Implementar mecanismos seguros de comunicação entre contextos sem criar dependências diretas de projetos.

#### 2.1 Eventos de Domínio
**Arquivo(s):** `GameStore.Shared/Events/` ou `TheThroneOfGames.Domain/Events/`

```csharp
public record UsuarioAtivadoEvent(Guid UsuarioId, string Email) : IDomainEvent;
public record GameCompradoEvent(Guid GameId, Guid UserId, decimal Price) : IDomainEvent;
```

**Uso:**
- Quando `UsuarioService.ActivateUserAsync()` é chamado, publica `UsuarioAtivadoEvent`
- Outros contextos escutam o evento sem depender direto de `GameStore.Usuarios`

**Implementação:** Use um event bus simples (ex: `IEventBus`) ou MediatR.

---

#### 2.2 APIs Internas (gRPC ou REST)
**Arquivo(s):** `GameStore.{Contexto}/API/`

Cada contexto expõe endpoints internos (não públicos):

```
POST /internal/usuarios/validate/{usuarioId}
GET  /internal/catalogo/games/{gameId}
GET  /internal/vendas/purchases/{usuarioId}
```

**Por quê:** Permite que contextos comuniquem sem acoplamento de dependências de projetos .NET.

---

### Fase 3: Migração a Microservices (4-8 semanas)

**Objetivo:** Separar cada contexto em um serviço independente.

#### 3.1 Separar DbContexts
- `GameStore.Usuarios/Infrastructure/Persistence/UsuariosDbContext.cs`
- `GameStore.Catalogo/Infrastructure/Persistence/CatalogoDbContext.cs`
- `GameStore.Vendas/Infrastructure/Persistence/VendasDbContext.cs`

#### 3.2 Bancos de Dados Independentes
- `usuários_db` (SQL Server ou PostgreSQL)
- `catalogo_db`
- `vendas_db`

#### 3.3 Containerização
```dockerfile
# Exemplo: GameStore.Usuarios/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .
EXPOSE 5001
ENTRYPOINT ["dotnet", "GameStore.Usuarios.dll"]
```

#### 3.4 Orquestração
```yaml
# docker-compose.yml
version: '3.8'
services:
  usuarios:
    build: ./GameStore.Usuarios
    ports:
      - "5001:5001"
    environment:
      - ConnectionStrings__Default=Server=usuarios_db;...
  catalogo:
    build: ./GameStore.Catalogo
    ports:
      - "5002:5002"
  vendas:
    build: ./GameStore.Vendas
    ports:
      - "5003:5003"
```

---

### Próximos Passos (Este Sprint)

- [ ] Implementar eventos de domínio (IDomainEvent, IEventBus)
- [ ] Criar endpoints internos para comunicação entre contextos
- [ ] Adicionar retry policies para chamadas internas
- [ ] Documentar contratos de eventos
- [ ] Setup de Kafka ou RabbitMQ para publicação de eventos
- [ ] Criar `GameStore.Shared` para DTOs e eventos compartilhados
- [ ] Code review com equipe

---

## Checklist de Entrega Final

- [x] Estrutura de bounded contexts criada
- [x] Projetos adicionados à solution
- [x] Build verde
- [x] Testes iniciais adicionados (GameStore.Usuarios.Tests)
- [x] Documentação atualizada
- [x] DTOs por contexto implementados
- [x] Mappers criados e testados
- [x] Cobertura de testes ≥80%
- [x] 3 projetos de testes (Usuarios, Catalogo, Vendas)
- [ ] Eventos de domínio implementados
- [ ] APIs internas documentadas
- [ ] DbContexts separados
- [ ] Containers criados
- [ ] Documentação de deployment

---

## Referências

- `ARCHITECTURE_README.md` — Visão geral da arquitetura
- `DEVELOPMENT_PROMPT.md` — Instruções para desenvolvimento
- `relatorio_entrega.txt` — Histórico completo de mudanças

**Data de atualização:** 25 de novembro de 2025

