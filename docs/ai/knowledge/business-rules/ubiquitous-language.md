# Ubiquitous Language — TheThroneOfGames (GameStore)

> Glossário oficial de domínio. O agente de IA usa este vocabulário em todo código gerado.
> Após editar: `python tools/embed-knowledge.py` para re-indexar no ChromaDB.

---

## Metadados do Projeto
```
Projeto: TheThroneOfGames (GameStore)
Domínio: E-commerce de jogos digitais
Bounded Contexts: Catalogo, Usuarios, Vendas
Última atualização: 2026-05-26
```

---

## 1. ENTIDADES PRINCIPAIS (Aggregate Roots)

| Termo de Negócio | Sinônimos Aceitos | Sinônimos PROIBIDOS | Descrição |
|---|---|---|---|
| `Jogo` | Jogo, Título | `Game, GameEntity, Product` | Software de entretenimento vendido na plataforma |
| `Usuario` | Usuário, Jogador | `User, Customer, UserEntity` | Pessoa cadastrada — pode ser cliente ou admin |
| `Pedido` | Pedido de Compra | `Order, Cart, Carrinho` | Intenção de compra formalizada com 1..N itens |

---

## 2. VALUE OBJECTS

| Termo de Negócio | Tipo | Regras de Validação | Exemplos Válidos | Exemplos Inválidos |
|---|---|---|---|---|
| `Preco` | `decimal + string (moeda)` | `>= 0, moeda não vazia` | `59.99 BRL` | `-1.00, "" BRL` |
| `Money` | `decimal + string (moeda)` | `>= 0` | `100.00 BRL` | `-5.00` |
| `Token de Ativação` | `string (UUID)` | `não nulo, não vazio` | `550e8400-...` | `""` |

---

## 3. AÇÕES DE NEGÓCIO (Use Cases / Commands)

| Termo de Negócio | Verbo | Quem pode executar | Pré-condições | Resultado Esperado |
|---|---|---|---|---|
| `Cadastrar Jogo` | `CreateGame` | `Admin` | `Nome único no catálogo, preço >= 0` | `Jogo criado, evento GameCriado emitido` |
| `Atualizar Jogo` | `UpdateGame` | `Admin` | `Jogo existe, novo nome não duplicado` | `Jogo atualizado, evento GameAtualizado emitido` |
| `Indisponibilizar Jogo` | `RemoveGame` | `Admin` | `Jogo existe` | `Jogo.Disponivel = false` |
| `Registrar Usuário` | `RegisterUser` | `Público` | `Email único` | `Usuário inativo criado, token de ativação gerado` |
| `Ativar Conta` | `ActivateUser` | `Usuário via email` | `Token válido` | `Usuário ativo, evento UsuarioAtivado emitido` |
| `Adicionar Item ao Pedido` | `AddItem` | `Usuário ativo` | `Pedido Pendente, Jogo não duplicado` | `Item adicionado, ValorTotal recalculado` |
| `Finalizar Pedido` | `FinalizeOrder` | `Usuário ativo` | `Pedido Pendente, min 1 item` | `Pedido Finalizado, evento PedidoFinalizado emitido` |

---

## 4. EVENTOS DE DOMÍNIO

| Evento | Quando Ocorre | Quem Consome | Payload Mínimo |
|---|---|---|---|
| `GameCriadoEvent` | Ao cadastrar um Jogo | Notificações | `{ GameId, Nome, Preco }` |
| `GameAtualizadoEvent` | Ao atualizar um Jogo | Notificações | `{ GameId, Nome, Preco }` |
| `UsuarioAtivadoEvent` | Ao confirmar email | Emails de boas-vindas | `{ UsuarioId, Email }` |
| `PedidoFinalizadoEvent` | Ao finalizar compra | Catálogo (decrementar estoque) | `{ PedidoId, UserId, TotalPrice, ItemCount }` |

---

## 5. STATUS E CICLO DE VIDA

| Entidade | Estados Possíveis | Transições Válidas | Estados Terminais |
|---|---|---|---|
| `Pedido` | `Pendente → Finalizado / Cancelado` | `Pendente→Finalizado (pagamento), Pendente→Cancelado (desistência)` | `Finalizado, Cancelado` |
| `Usuario` | `Inativo → Ativo / Desativado` | `Inativo→Ativo (ativação email), Ativo→Desativado (admin)` | `Desativado` |
| `Jogo` | `Disponivel = true/false` | `true→false (Indisponibilizar ou estoque=0)` | — |

---

## 6. REGRAS DE NEGÓCIO EXPLÍCITAS

```
[RN-001] Um Jogo com Estoque = 0 é automaticamente marcado como Disponivel = false.
[RN-002] Um Pedido só pode ter o mesmo Jogo adicionado uma única vez.
[RN-003] Um Pedido não-Pendente não aceita novos Itens nem remoções.
[RN-004] Um Pedido sem Itens não pode ser Finalizado.
[RN-005] Um Usuário recém-cadastrado começa sempre como IsActive = false até Ativação.
[RN-006] O Preço de um Jogo nunca pode ser negativo (>= 0 permitido para jogo gratuito).
[RN-007] Admin pode Indisponibilizar um Jogo mesmo com estoque positivo.
```

---

## 7. TERMOS PROIBIDOS (Anti-Corruption Layer)

| Proibido | Usar em vez | Motivo |
|---|---|---|
| `GameEntity` | `Jogo` | Entidade antiga — migrada para Bounded Context |
| `IGameRepository` | `IJogoRepository` | Interface antiga — migrada |
| `Name` (em Jogo) | `Nome` | Propriedade em português conforme DDD do contexto |
| `Price` (em Jogo) | `Preco` | Idem |
| `IsAvailable` (em Jogo) | `Disponivel` | Idem |

---

## 8. BOUNDED CONTEXTS E RELACIONAMENTOS

```
┌───────────────────┐   GameCriadoEvent      ┌─────────────────────┐
│   CATALOGO         │ ──────────────────────► │   (Notificações)    │
│  (Jogos, Preços)   │                         └─────────────────────┘
└───────────────────┘
         ▲
         │ PedidoFinalizadoEvent (decrementar estoque)
         │
┌───────────────────┐   UsuarioAtivadoEvent  ┌─────────────────────┐
│   VENDAS           │ ◄────────────────────── │   USUARIOS          │
│  (Pedidos, Itens)  │                         │  (Auth, Perfis)     │
└───────────────────┘                         └─────────────────────┘
```
