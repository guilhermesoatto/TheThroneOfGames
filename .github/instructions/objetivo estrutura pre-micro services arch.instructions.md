---
applyTo: '**'
---
Devemos separar nosso projeto em contextos limitados (bounded contexts) para garantir que cada parte do sistema tenha uma responsabilidade clara e bem definida. A seguir está uma sugestão de estrutura para o projeto FIAP Cloud Games, dividida em três contextos principais: Usuários, Catálogo e Vendas.

```
A estrutura abaixo ilustra como organizar os diretórios e arquivos dentro de cada contexto limitado:

GameStore.Catalogo/
│
├── Domain/
│   ├── Entities/
│   │   └── Jogo.cs
│   ├── ValueObjects/
│   │   └── Preco.cs
│   └── Repositories/
│       └── IJogoRepository.cs
│
├── Application/
│   ├── Commands/
│   │   └── CriarJogoCommand.cs
│   ├── Handlers/
│   │   └── CriarJogoHandler.cs
│   └── DTOs/
│       └── JogoDto.cs
│
└── Infrastructure/
    ├── Persistence/
    │   ├── CatalogoDbContext.cs
    │   ├── Configurations/
    │   │   └── JogoConfiguration.cs
    │   └── JogoRepository.cs
    └── Extensions/
        └── CatalogoInfrastructureExtensions.cs
├── GameStore.Vendas/                 → Contexto de Vendas
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   │   ├── Pedido.cs
│   │   │   │   └── ItemPedido.cs
│   │   │   ├── ValueObjects/
│   │   │   │   └── Money.cs
│   │   │   ├── Events/
│   │   │   │   └── PedidoFinalizadoEvent.cs
│   │   │   └── Repositories/IPedidoRepository.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── AdicionarItemCommand.cs
│   │   │   │   └── FinalizarPedidoCommand.cs
│   │   │   └── Handlers/
│   │   │       └── FinalizarPedidoHandler.cs
│   │   └── Infrastructure/
│   │       ├── Persistence/
│   │       │   ├── VendasDbContext.cs
│   │       │   └── PedidoRepository.cs
│   │       └── Messaging/
│   │           └── EventPublisher.cs

Vamos detalhar cada contexto:
1. Contexto de Usuários (GameStore.Usuarios)
   - Responsável pelo gerenciamento de usuários, autenticação e autorização.
   - Inclui entidades como Usuário, Perfil e Roles.
   - Serviços para autenticação JWT e gerenciamento de senhas.
2. Contexto de Catálogo (GameStore.Catalogo)
   - Gerencia os jogos disponíveis na plataforma.
   - Inclui entidades como Jogo, Categoria e Avaliação.
   - Serviços para busca, filtragem e recomendação de jogos.
3. Contexto de Vendas (GameStore.Vendas)
   - Responsável pelo processo de compra e gerenciamento de pedidos.
   - Inclui entidades como Pedido, ItemPedido e Cartão de Crédito.
   - Serviços para processamento de pagamentos e gerenciamento de estoque.

Cada contexto deve ser desenvolvido de forma independente, permitindo que equipes diferentes trabalhem simultaneamente em cada parte do sistema. A comunicação entre os contextos pode ser feita através de eventos ou APIs, garantindo a integridade e a coesão do sistema como um todo.

Nosso objetivo ao estruturar o projeto dessa forma é facilitar a manutenção, escalabilidade e evolução do sistema ao longo do tempo, alinhando-se às melhores práticas de arquitetura de software.

E também preparar o terreno para uma futura migração para uma arquitetura de microservices, onde cada contexto pode ser transformado em um serviço independente conforme a necessidade.
```