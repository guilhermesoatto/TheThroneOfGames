# TheThroneOfGames

## Visão Geral
TheThroneOfGames é uma API Web moderna e segura em ASP.NET Core para gerenciar uma plataforma digital de jogos. Este projeto foi desenvolvido como solução para o desafio Tech Challenge (ver `TheThroneOfGames.API/objetivo1.md`), atendendo todos os requisitos obrigatórios da primeira fase e evoluindo para uma arquitetura de **bounded contexts** independente, preparando o terreno para uma futura migração para microservices.

## Funcionalidades
- Registro de usuário com ativação por e-mail
- Validação de força de senha e hash seguro PBKDF2
- Autenticação JWT com claims de papel (role)
- Endpoints de administração para gerenciar jogos, promoções e usuários
- Integração com EF Core e SQL Server
- Envio automatizado de e-mails (simulado para arquivos no dev/teste)
- Testes unitários e de integração abrangentes (NUnit)
- Tratamento global de exceções e respostas ProblemDetails
- Documentação Swagger/OpenAPI
- **Arquitetura de Bounded Contexts**: Separação clara entre domínios de Usuários, Catálogo e Vendas
- **Comunicação Event-Driven**: Eventos de domínio entre contextos via IEventBus
- **CQRS Pattern**: Commands e Queries para operações de domínio
- **Busca avançada de jogos via Elasticsearch**: `GET /api/game/search?q=` — full-text search por nome, gênero e descrição, com fallback automático para busca no banco caso o Elasticsearch esteja indisponível

## Stack Tecnológico
- ASP.NET Core 9.0 Web API
- Entity Framework Core
- SQL Server (localdb ou completo)
- NUnit (testes unitários/integrados)
- Serilog (recomendado para logs em produção)
- Docker (opcional)
- **Arquitetura**: Domain-Driven Design (DDD) com Bounded Contexts
- **Padrões**: CQRS, Event Sourcing (preparado), Repository Pattern
- **Comunicação**: Event-Driven Architecture com SimpleEventBus

## Primeiros Passos

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (localdb ou completo)
- Docker Desktop (para execução local completa)

### 🚀 Início Rápido - Execução Local (Recomendado)

A forma mais rápida de executar o projeto completo com todas as dependências:

```powershell
cd scripts
.\run-local.ps1 -LoadData
```

Este comando irá:
- Iniciar SQL Server, RabbitMQ, Prometheus e Grafana via Docker
- Iniciar as 3 APIs de microservices (Usuarios, Catalogo, Vendas)
- Carregar dados iniciais (usuários, jogos, pedidos)

### 📊 Validação e Testes (Fase 4)

**Validação Rápida (15 checks em 2 min):**
```powershell
cd scripts
.\validation-checklist.ps1 -Mode quick
```

**Validação Completa (22 checks em 5 min):**
```powershell
.\validation-checklist.ps1 -Mode full -GenerateReport
```
Gera relatório em `validation-report-TIMESTAMP.txt`

**Teste de Carga (100% cobertura de endpoints):**
```powershell
.\load-test.ps1 -GenerateReport
# ou com parâmetros reduzidos:
.\load-test.ps1 -NumUsuarios 10 -NumJogos 20 -NumPedidos 30 -ConcurrentUsers 3
```

**Validação Kubernetes (quando disponível):**
```powershell
.\validation-checklist.ps1 -Mode k8s
```

### 📈 Monitoramento em Tempo Real

Após iniciar a aplicação, acesse:
- **Grafana Dashboard**: http://localhost:3000 (admin/admin)
  - Métricas: CPU, Memory, Network, HTTP latency
  - RabbitMQ messages e Dead Letter Queue monitoring
- **RabbitMQ Management UI**: http://localhost:15672 (guest/guest)
  - Filas, exchanges, queues e retry policies
- **Prometheus Metrics**: http://localhost:9090
  - Query de métricas raw
- Exibir todas as URLs de acesso

**Serviços disponíveis:**
- 📊 Grafana: http://localhost:3000 (admin/admin)
- 📈 Prometheus: http://localhost:9090
- 🐰 RabbitMQ: http://localhost:15672 (guest/guest)
- 👥 Usuarios API: http://localhost:5001/swagger
- 🎮 Catalogo API: http://localhost:5002/swagger
- 🛒 Vendas API: http://localhost:5003/swagger

Para mais detalhes, consulte [LOCAL_EXECUTION_GUIDE.md](LOCAL_EXECUTION_GUIDE.md)

### Configuração Manual (Desenvolvimento)
1. **Clone o repositório:**
   ```sh
   git clone <seu-repo-url>
   cd TheThroneOfGames
   ```
2. **Configure as variáveis de ambiente:**
   - Copie o `appsettings.Development.json` conforme necessário e defina seu segredo JWT e string de conexão do banco.
   - Para produção, use variáveis de ambiente ou um arquivo `.env` (veja abaixo).

3. **Restaure as dependências:**
   ```sh
   dotnet restore
   ```

### Migrações do Banco de Dados
1. **Aplique as migrações:**
   ```sh
   dotnet ef database update --project TheThroneOfGames.Infrastructure --startup-project TheThroneOfGames.API
   ```
   Isso criará o banco de dados e aplicará todas as migrações.

### Executando a Aplicação
1. **Inicie a API:**
   ```sh
   dotnet run --project TheThroneOfGames.API
   ```
   A API estará disponível em `https://localhost:5001` (ou conforme configurado).

2. **Swagger UI:**
   Acesse `https://localhost:5001/swagger` para documentação interativa e testes da API.

### Testes
1. **Execute todos os testes:**
   ```sh
   dotnet test
   ```
   Isso executará todos os testes unitários e de integração, incluindo ativação por e-mail, validação de senha, JWT e cenários de autorização.

### Docker (Opcional)
1. **Build e execução com Docker:**
   ```sh
   docker build -t thethroneofgames .
   docker run -p 5001:5001 thethroneofgames
   ```
   (Garanta que sua string de conexão e segredo JWT estejam definidos via variáveis de ambiente ou segredos Docker.)

### Testando Endpoints com Swagger
1. **Registre um usuário:**
   - No Swagger UI (`https://localhost:5001/swagger`), localize o endpoint `/api/Usuario/pre-register`
   - Use o modelo de exemplo para registrar um usuário com email e senha válidos:
     ```json
     {
       "name": "Usuário Teste",
       "email": "teste@exemplo.com",
       "password": "Senha@123",
       "role": "User"
     }
     ```
   - Verifique a pasta `Infrastructure/Outbox` para o email de ativação
   - Use o token de ativação no endpoint `/api/Usuario/activate`

2. **Obtenha um token JWT:**
   - Use o endpoint `/api/Usuario/login` com o email e senha registrados:
     ```json
     {
       "email": "teste@exemplo.com",
       "password": "Senha@123"
     }
     ```
   - Copie o token JWT retornado na resposta

3. **Use o token no Swagger:**
   - No topo da página do Swagger, clique no botão "Authorize" ou no cadeado
   - No campo "Value", digite: `Bearer seu-token-jwt`
   - Clique em "Authorize" e feche o modal
   - Agora você pode acessar endpoints protegidos

4. **Para endpoints administrativos:**
   - É necessário um usuário com role "Admin"
   - Por padrão, o primeiro usuário pode ser promovido a admin via banco de dados
   - Ou use o endpoint de promoção de usuário (requer um admin existente)

## Configuração
- **Segredo JWT:** Defina em `appsettings.json` ou como variável de ambiente `JWT_SECRET`.
- **Conexão com Banco:** Defina em `appsettings.json` ou como variável de ambiente `DB_CONNECTION`.
- **Outbox de E-mail:** Para desenvolvimento, e-mails são gravados em `Infrastructure/Outbox` como arquivos `.eml`.

## Relatório de Entrega
- **Segurança**: Senhas validadas quanto à força e armazenadas com hash PBKDF2. Tokens JWT incluem claims de papel e expiração. Endpoints de administração são protegidos por autorização baseada em papel.
- **Testes**: 104 testes unitários passando (61 Usuários + 43 Catálogo), cobertura completa dos bounded contexts. Testes de infraestrutura (RabbitMQ) falham quando serviço não está disponível.
- **Arquitetura**: Migração completa para bounded contexts com comunicação event-driven. Pronto para evolução para microservices.
- **Qualidade**: Princípios DDD aplicados, CQRS implementado, separação clara de responsabilidades, mappers para conversão de DTOs.
- **Extensibilidade**: Arquitetura preparada para adição de novos bounded contexts, escalabilidade horizontal e deployment independente.

## Roadmap para Microservices
**Fase Atual**: Bounded Contexts implementados e funcionais ✅
- ✅ Separação de domínios
- ✅ Interfaces locais por contexto
- ✅ Comunicação via eventos
- ✅ Testes independentes
- ✅ Configuração flexível de event bus (SimpleEventBus/RabbitMQ)
- ✅ Containerização básica com Docker
- ✅ Docker Compose com RabbitMQ e SQL Server

**Próximas Fases**:
- **Separação de Microsserviços**: Extrair APIs independentes por contexto
- **Mensageria Completa**: Implementar consumers dedicados para RabbitMQ
- **Bancos Independentes**: Separar DbContexts e criar bancos por serviço
- **Orquestração Avançada**: Kubernetes manifests, HPA, ConfigMaps/Secrets
- **Monitoramento**: Prometheus/Grafana, APM, logs distribuídos
- **CI/CD**: Pipelines independentes por microsserviço

## Arquitetura de Bounded Contexts

O projeto foi refatorado para seguir os princípios de Domain-Driven Design (DDD) com **Bounded Contexts** independentes, preparando o terreno para uma futura migração para microservices:

### GameStore.Usuarios (Contexto de Usuários)
**Responsabilidade**: Gerenciamento completo de usuários, autenticação, perfis e roles.
- **Domínio**: Usuario.cs, ValueObjects (Email, Senha), Events (UsuarioAtivadoEvent, UsuarioPerfilAtualizadoEvent)
- **Aplicação**: Commands (CriarUsuario, AtivarUsuario), Queries, Handlers CQRS
- **Infraestrutura**: UsuarioRepository, UsuarioDbContext, Mappers
- **Testes**: 61 testes unitários cobrindo todas as funcionalidades

### GameStore.Catalogo (Contexto de Catálogo)
**Responsabilidade**: Gerenciamento do catálogo de jogos, CRUD operations, disponibilidade e busca avançada.
- **Domínio**: Jogo.cs, ValueObjects (Preco), Events (GameCompradoEvent)
- **Aplicação**: Commands (CriarJogo, AtualizarJogo), Queries (inclui `SearchGamesQuery`), Handlers CQRS
- **Infraestrutura**: JogoRepository, CatalogoDbContext, `ElasticsearchJogoIndexer` (indexação em Create/Update/Remove), Mappers
- **Busca**: `GET /api/game/search?q=<termo>` — full-text via Elasticsearch (nome, gênero, descrição); cai automaticamente para busca no banco se o Elasticsearch estiver fora do ar
- **Testes**: cobertura unitária + integração real contra RabbitMQ e Elasticsearch via Testcontainers

### GameStore.Vendas (Contexto de Vendas)
**Responsabilidade**: Processamento de pedidos, compras e transações.
- **Domínio**: Pedido.cs, ItemPedido.cs, ValueObjects (Money), Events (PedidoFinalizadoEvent)
- **Aplicação**: Commands (AdicionarItem, FinalizarPedido), Queries, Handlers CQRS
- **Infraestrutura**: PedidoRepository, VendasDbContext, Mappers
- **Testes**: Implementação completa com testes unitários

### Comunicação Entre Contextos
- **Event-Driven Architecture**: IEventBus com SimpleEventBus para comunicação assíncrona
- **Event Handlers**: Processamento de eventos entre contextos (ex: UsuarioAtivadoEvent → Catalogo)
- **Integração**: API principal registra todos os contextos e configura event handlers

## Status do Projeto - FASE 4 CONCLUÍDA ✅

### Fase 4: Produção & Infraestrutura (COMPLETA)
- ✅ **Comunicação Assíncrona**: RabbitMQ com retry policies (5s → 25s → 125s) e Dead Letter Queue
- ✅ **Docker Otimizado**: Multi-stage builds, imagens ~450MB, segurança (non-root)
- ✅ **Kubernetes**: 24+ manifestos, HPA (3-10 replicas), StatefulSets, Network Policies
- ✅ **Monitoramento**: Prometheus (15s scrape) + Grafana dashboards + Health checks
- ✅ **Validação**: 86.4% sucesso (19/22 verificações automáticas)
- ✅ **Load Testing**: 100% cobertura de endpoints, teste de carga concorrente
- ✅ **Documentação**: 
  - [FASE4_COMPLETION_SUMMARY.md](docs/FASE4_COMPLETION_SUMMARY.md) - Resumo completo
  - [FASE4_ASYNC_FLOW.md](docs/FASE4_ASYNC_FLOW.md) - Arquitetura de eventos (600+ linhas)
  - [ARQUITETURA_K8s.md](docs/ARQUITETURA_K8s.md) - Orquestração Kubernetes (800+ linhas)
  - [PROXIMOS_PASSOS_FASE5.md](docs/PROXIMOS_PASSOS_FASE5.md) - Roadmap Fase 5

### Fase Anterior: Fase 3 (COMPLETA)
- ✅ **Build**: Sucesso (compilação limpa)
- ✅ **Testes**: 104/116 testes passando (61 Usuários + 43 Catálogo)
- ✅ **Funcionalidades**: Todos os requisitos do Tech Challenge atendidos
- ✅ **Arquitetura**: Bounded contexts implementados e funcionais
- ✅ **Event-Driven**: Comunicação entre contextos estabelecida
- ✅ **CQRS**: Padrão implementado em todos os contextos

## Estrutura do Projeto
```
TheThroneOfGames.sln
├── TheThroneOfGames.API/          # API principal e configuração
├── GameStore.Usuarios/             # Bounded Context: Usuários
│   ├── Domain/                     # Entidades, ValueObjects, Events
│   ├── Application/                # Commands, Queries, Handlers, DTOs
│   └── Infrastructure/             # Repositories, DbContext, Mappers
├── GameStore.Catalogo/             # Bounded Context: Catálogo
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── GameStore.Vendas/               # Bounded Context: Vendas
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── GameStore.Common/               # Componentes compartilhados
├── GameStore.CQRS.Abstractions/    # Abstrações CQRS
├── Test/                           # Testes de integração (monólito)
└── [Bounded Context].Tests/        # Testes unitários por contexto
```

## Referências
- Requisitos do desafio: veja `TheThroneOfGames.API/objetivo1.md`
- Arquitetura de Bounded Contexts: veja `.github/instructions/objetivo estrutura pre-micro services arch.instructions.md`
- Relatório de entrega detalhado: veja `relatorio_entrega.txt`
- Melhorias propostas: veja `docs/melhorias_propostas.md`
- Passos para finalização: veja `docs/FINISHING_STEPS.md`

## Desenvolvimento com Bounded Contexts

### Trabalhando com Contextos
Cada bounded context é independente e pode ser desenvolvido separadamente:

```bash
# Desenvolvimento focado em um contexto
cd GameStore.Usuarios
dotnet build
dotnet test

# API principal integra todos os contextos
cd TheThroneOfGames.API
dotnet run
```

### Configuração de Mensageria
O projeto suporta dois modos de event bus:

**Modo Desenvolvimento (SimpleEventBus):**
```json
{
  "EventBus": {
    "UseRabbitMq": false
  }
}
```

**Modo Produção (RabbitMQ):**
```json
{
  "EventBus": {
    "UseRabbitMq": true,
    "RabbitMq": {
      "Host": "localhost",
      "Port": 5672,
      "Username": "guest",
      "Password": "guest"
    }
  }
}
```

### Containerização e Orquestração
Para executar com Docker e RabbitMQ:

```bash
# Construir e executar com docker-compose
docker-compose up --build

# Acessar RabbitMQ Management UI
# http://localhost:15672 (guest/guest)
```

### Adicionando Novos Eventos
1. Defina o evento no contexto de origem (`Domain/Events/`)
2. Implemente o handler no contexto de destino (`Application/EventHandlers/`)
3. Para RabbitMQ: Crie um consumer separado para processar mensagens da fila

### Testes por Contexto
- Execute testes de um contexto específico: `dotnet test GameStore.Usuarios.Tests`
- Testes de integração continuam no projeto `Test/`
- Cobertura: 104/116 testes passando (61 Usuários + 43 Catálogo, excluindo testes de infraestrutura externa)

## Contribuindo
Pull requests e issues são bem-vindos! Por favor, garanta que todos os testes passem e siga o estilo de código existente.

**Para desenvolvimento em bounded contexts:**
- Mantenha interfaces locais (não referencie outros contextos diretamente)
- Use eventos para comunicação entre contextos
- Adicione testes unitários para novas funcionalidades
- Atualize mappers e DTOs conforme necessário

## Licença
Licença MIT

---

Para dúvidas ou suporte, entre em contato com o mantenedor.
