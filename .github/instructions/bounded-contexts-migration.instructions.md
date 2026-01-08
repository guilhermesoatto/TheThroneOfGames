---
applyTo: '**'
---

# Migração para Arquitetura de Bounded Contexts

## Contexto
Este projeto foi migrado de uma arquitetura monolítica para uma arquitetura baseada em Bounded Contexts (DDD), preparando o terreno para futura migração para microservices.

## Estrutura de Bounded Contexts

### 1. GameStore.Catalogo
- **Responsabilidade**: Gerenciamento do catálogo de jogos
- **Entidade Principal**: `Jogo` (não `GameEntity`)
- **Interface Repository**: `IJogoRepository` (não `IGameRepository`)
- **Propriedades da Entidade Jogo**:
  - `Id` (Guid)
  - `Nome` (string) - português, não "Name"
  - `Descricao` (string) - português, não "Description"
  - `Preco` (decimal) - português, não "Price"
  - `Genero` (string) - português, não "Genre"
  - `Desenvolvedora` (string)
  - `DataLancamento` (DateTime)
  - `Disponivel` (bool) - português, não "IsAvailable"
  - `ImagemUrl` (string)
  - `Estoque` (int)

### 2. GameStore.Usuarios
- **Responsabilidade**: Gerenciamento de usuários e autenticação
- **Entidade Principal**: `Usuario` (do bounded context, não do domínio antigo)
- **Interface Repository**: `IUsuarioRepository` (do bounded context)
- **Namespace Correto**: `GameStore.Usuarios.Domain.Entities.Usuario`
- **Namespace Incorreto**: ❌ `TheThroneOfGames.Domain.Entities.Usuario`

### 3. GameStore.Vendas
- **Responsabilidade**: Gerenciamento de pedidos e vendas
- **Entidades**: Pedido, ItemPedido
- **Namespace**: `GameStore.Vendas.Domain.Entities`

## Regras Críticas para Desenvolvimento

### ✅ O QUE FAZER

1. **Imports Corretos**:
   ```csharp
   // CORRETO - Bounded Context
   using GameStore.Catalogo.Domain.Entities;
   using GameStore.Catalogo.Domain.Interfaces;
   
   // CORRETO - Bounded Context Usuarios
   using GameStore.Usuarios.Domain.Entities;
   using GameStore.Usuarios.Domain.Interfaces;
   ```

2. **Uso de Entidades**:
   ```csharp
   // CORRETO - Usar entidade do bounded context
   var jogo = new Jogo(
       nome: "The Witcher 3",
       descricao: "RPG de mundo aberto",
       preco: 59.99m,
       genero: "RPG",
       desenvolvedora: "CD Projekt Red",
       dataLancamento: DateTime.Now,
       imagemUrl: "",
       estoque: 100
   );
   ```

3. **Métodos de Domínio**:
   ```csharp
   // CORRETO - Usar métodos do domínio
   jogo.AtualizarInformacoes(nome, descricao, preco, genero, desenvolvedora, imagemUrl);
   jogo.Indisponibilizar();
   jogo.AtualizarEstoque(novoEstoque);
   ```

4. **Repositories**:
   ```csharp
   // CORRETO - Usar interface do bounded context
   private readonly IJogoRepository _jogoRepository;
   
   // CORRETO - Métodos retornam IEnumerable
   var jogos = await _jogoRepository.GetByNomeAsync(nome); // IEnumerable<Jogo>
   var jogo = jogos.FirstOrDefault();
   ```

### ❌ O QUE NÃO FAZER

1. **Imports Incorretos**:
   ```csharp
   // ❌ ERRADO - Domínio antigo monolítico
   using TheThroneOfGames.Domain.Entities;
   using TheThroneOfGames.Domain.Interfaces;
   ```

2. **Entidades Antigas**:
   ```csharp
   // ❌ ERRADO - Entity antiga
   var game = new GameEntity { 
       Name = "Game",
       Price = 59.99m
   };
   ```

3. **Propriedades em Inglês no Jogo**:
   ```csharp
   // ❌ ERRADO - Propriedades não existem
   jogo.Name = "teste";
   jogo.Price = 59.99m;
   jogo.IsAvailable = true;
   
   // ✅ CORRETO - Propriedades em português
   jogo.Nome // leitura
   jogo.Preco // leitura
   jogo.Disponivel // leitura
   ```

4. **Object Initializer para Jogo**:
   ```csharp
   // ❌ ERRADO - Jogo usa constructor
   var jogo = new Jogo {
       Nome = "teste",
       Preco = 59.99m
   };
   
   // ✅ CORRETO - Usar constructor
   var jogo = new Jogo(nome, descricao, preco, genero, desenvolvedora, dataLancamento, imagemUrl, estoque);
   ```

## Mapeamento de Propriedades

### GameEntity (antigo) → Jogo (novo)

| Antiga (GameEntity) | Nova (Jogo) | Tipo |
|---------------------|-------------|------|
| Name | Nome | string |
| Genre | Genero | string |
| Price | Preco | decimal |
| Description | Descricao | string |
| IsAvailable | Disponivel | bool |
| CreatedAt | DataLancamento | DateTime |
| UpdatedAt | ❌ Não existe | - |
| ❌ Não existe | Desenvolvedora | string |
| ❌ Não existe | ImagemUrl | string |
| ❌ Não existe | Estoque | int |

## Padrões de Implementação

### Handlers

```csharp
public class CreateGameCommandHandler : ICommandHandler<CreateGameCommand>
{
    private readonly IJogoRepository _jogoRepository; // Bounded context
    private readonly IEventBus _eventBus;

    public async Task<CommandResult> HandleAsync(CreateGameCommand command)
    {
        // Verificar duplicatas
        var existingGames = await _jogoRepository.GetByNomeAsync(command.Name);
        if (existingGames.Any())
        {
            return new CommandResult { Success = false, Message = "Jogo já existe" };
        }

        // Criar usando constructor
        var jogo = new Jogo(
            nome: command.Name,
            descricao: command.Description ?? "Sem descrição",
            preco: command.Price,
            genero: command.Genre,
            desenvolvedora: "Unknown",
            dataLancamento: DateTime.UtcNow,
            imagemUrl: "",
            estoque: 100
        );

        await _jogoRepository.AddAsync(jogo);
        
        return new CommandResult 
        { 
            Success = true, 
            EntityId = jogo.Id,
            Data = GameMapper.ToDTO(jogo)
        };
    }
}
```

### Queries

```csharp
public class GetGameByNameQueryHandler : IQueryHandler<GetGameByNameQuery, GameDTO?>
{
    private readonly IJogoRepository _jogoRepository;

    public async Task<GameDTO?> HandleAsync(GetGameByNameQuery query)
    {
        var jogos = await _jogoRepository.GetByNomeAsync(query.Name);
        var jogo = jogos.FirstOrDefault();
        return jogo != null ? GameMapper.ToDTO(jogo) : null;
    }
}
```

### Mappers

```csharp
public static class GameMapper
{
    public static GameDTO ToDTO(Jogo jogo)
    {
        return new GameDTO
        {
            Id = jogo.Id,
            Name = jogo.Nome,           // Mapear português → inglês
            Price = jogo.Preco,
            Genre = jogo.Genero,
            Description = jogo.Descricao,
            IsAvailable = jogo.Disponivel,
            CreatedAt = jogo.DataLancamento,
            UpdatedAt = jogo.DataLancamento
        };
    }

    public static Jogo FromDTO(GameDTO dto)
    {
        return new Jogo(
            nome: dto.Name,
            descricao: dto.Description ?? "",
            preco: dto.Price,
            genero: dto.Genre,
            desenvolvedora: "Unknown",
            dataLancamento: dto.CreatedAt,
            imagemUrl: "",
            estoque: 100
        );
    }
}
```

## Testes Unitários

### Setup para Mocks

```csharp
// CORRETO - Mock do bounded context
var mockJogoRepository = new Mock<IJogoRepository>();

// CORRETO - Setup retornando entidade do bounded context
mockJogoRepository
    .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(new Jogo(
        nome: "Test Game",
        descricao: "Test",
        preco: 59.99m,
        genero: "RPG",
        desenvolvedora: "Test Dev",
        dataLancamento: DateTime.Now,
        imagemUrl: "",
        estoque: 100
    ));

// CORRETO - Setup para GetByNomeAsync (retorna IEnumerable)
mockJogoRepository
    .Setup(x => x.GetByNomeAsync(It.IsAny<string>()))
    .ReturnsAsync(new List<Jogo>());
```

### Criar Entidades de Teste

```csharp
// CORRETO - Factory method para testes
private Jogo CreateTestJogo(string nome = "Test Game")
{
    return new Jogo(
        nome: nome,
        descricao: "Descrição de teste",
        preco: 59.99m,
        genero: "RPG",
        desenvolvedora: "Test Developer",
        dataLancamento: DateTime.UtcNow,
        imagemUrl: "http://test.com/image.jpg",
        estoque: 100
    );
}
```

## Migrations

As migrations antigas podem ter referências às entidades do domínio antigo. Para migrations novas:

1. Usar as entidades dos bounded contexts
2. Nomear tabelas de acordo com o contexto (ex: "Jogos" para Catalogo)
3. Aplicar migrations separadamente para cada DbContext

## Checklist de Migração

Ao trabalhar com código existente, verificar:

- [ ] Imports usam namespaces dos bounded contexts
- [ ] Entidades são dos bounded contexts (Jogo, não GameEntity)
- [ ] Repositories são dos bounded contexts (IJogoRepository)
- [ ] Propriedades usam nomes em português (Nome, Preco, Genero)
- [ ] Constructors são usados, não object initializers
- [ ] Métodos de domínio são usados (AtualizarInformacoes, Indisponibilizar)
- [ ] GetByNomeAsync retorna IEnumerable<Jogo>
- [ ] Mappers convertem corretamente entre português e inglês

## Estado Atual

✅ **Compilando com Sucesso**:
- GameStore.Catalogo
- GameStore.Usuarios  
- GameStore.Vendas
- TheThroneOfGames.API
- Test (Testes de Integração)

⚠️ **Pendentes**:
- GameStore.Catalogo.Tests - Testes unitários precisam ser atualizados
- GameStore.Usuarios.Tests - Testes unitários precisam ser atualizados
