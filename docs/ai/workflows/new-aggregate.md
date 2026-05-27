# [WORKFLOW: NEW AGGREGATE (RALPH AUTONOMY LOOP)]

**Gatilho:** Acionado sempre que for necessário criar um novo domínio, microserviço, ou feature complexa.

## A FILOSOFIA "RALPH" (Anti-Context Rot)
Como um Agente de IA, você NUNCA deve tentar codificar todo o Domínio, Use Cases e Adapters de uma só vez. Isso causa degradação de contexto. A execução deve ser estritamente iterativa. Trate cada passo como um loop isolado, baseando sua "memória" em arquivos no disco e não no histórico desta conversa.

## FASE 1: GERAÇÃO DO ESTADO (O PRD)
Antes de escrever qualquer linha de código C#:
1. Crie um arquivo de estado em `docs/ai/tasks/prd-[nome-do-dominio].json`.
2. Quebre o requisito solicitado pelo Arquiteto em micro-tarefas (User Stories). Cada tarefa DEVE ser pequena o suficiente para ser concluída em apenas um loop de contexto.
3. A ordem de execução estrita do DDD (De dentro para fora) deve ser mapeada no JSON:
   - [1] Criar Value Objects + Testes xUnit.
   - [2] Criar Entidades e Domain Events + Testes xUnit.
   - [3] Criar Interfaces de Aplicação (Ports).
   - [4] Criar Use Cases + Testes com NSubstitute.
   - [5] Criar Adapters (Infrastructure/WebApi) + Testes de Integração com Testcontainers.NET + WebApplicationFactory.

### Scaffold Inicial da Solução .NET:
Durante a Fase 1, o agente DEVE criar a estrutura da solução:
```bash
dotnet new sln -n NomeDoAggregate
dotnet new classlib -n NomeDoAggregate.Domain -o src/NomeDoAggregate.Domain
dotnet new classlib -n NomeDoAggregate.Application -o src/NomeDoAggregate.Application
dotnet new classlib -n NomeDoAggregate.Infrastructure -o src/NomeDoAggregate.Infrastructure
dotnet new webapi -n NomeDoAggregate.WebApi -o src/NomeDoAggregate.WebApi
dotnet new xunit -n NomeDoAggregate.Domain.Tests -o tests/NomeDoAggregate.Domain.Tests
dotnet new xunit -n NomeDoAggregate.Application.Tests -o tests/NomeDoAggregate.Application.Tests
dotnet new xunit -n NomeDoAggregate.Integration.Tests -o tests/NomeDoAggregate.Integration.Tests

# Adicionar projetos à solução
dotnet sln add src/NomeDoAggregate.Domain
dotnet sln add src/NomeDoAggregate.Application
dotnet sln add src/NomeDoAggregate.Infrastructure
dotnet sln add src/NomeDoAggregate.WebApi
dotnet sln add tests/NomeDoAggregate.Domain.Tests
dotnet sln add tests/NomeDoAggregate.Application.Tests
dotnet sln add tests/NomeDoAggregate.Integration.Tests

# Configurar referências entre projetos (Hexagonal)
dotnet add src/NomeDoAggregate.Application reference src/NomeDoAggregate.Domain
dotnet add src/NomeDoAggregate.Infrastructure reference src/NomeDoAggregate.Application
dotnet add src/NomeDoAggregate.WebApi reference src/NomeDoAggregate.Application
dotnet add src/NomeDoAggregate.WebApi reference src/NomeDoAggregate.Infrastructure
dotnet add tests/NomeDoAggregate.Domain.Tests reference src/NomeDoAggregate.Domain
dotnet add tests/NomeDoAggregate.Application.Tests reference src/NomeDoAggregate.Application
dotnet add tests/NomeDoAggregate.Integration.Tests reference src/NomeDoAggregate.WebApi
```

### Directory.Build.props (criar na raiz da solução):
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-all</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### Testes ArchUnitNET (criar durante a Fase 1):
O agente DEVE criar os testes de pureza de domínio conforme definido em `dotnet-clean-arch.md §5`.

## FASE 2: O LOOP DE EXECUÇÃO (Stateless Execution)
Para cada tarefa com o status `passes: false` no arquivo `prd.json`, siga este fluxo:
1. **Foco Estrito:** Leia as regras em `dotnet-clean-arch.md`. Implemente APENAS o código exigido na tarefa atual. Ignore o resto do escopo.
2. **Qualidade Embutida:** Escreva o teste xUnit correspondente à tarefa atual (usando FluentAssertions conforme `xunit-native-testing.md`).
3. **O Portão (Feedback Loop):** Execute internamente a validação de compilação (`dotnet build --no-restore`) e o teste criado (`dotnet test --no-build --filter "FullyQualifiedName~NomeDaTarefa"`).
4. **Auto-Correção:** Se quebrar, corrija o código imediatamente. É PROIBIDO avançar para a próxima tarefa se o estado atual do código não compilar ou o teste falhar.
5. **Avanço:** Se passar, altere a tarefa para `passes: true` no `prd.json`, documente rapidamente o que foi feito no arquivo `docs/ai/tasks/progress.txt` (a sua memória persistente) e prepare-se para o próximo loop.

## FASE 3: HUMAN-IN-THE-LOOP
Ao finalizar uma micro-tarefa com sucesso (CI verde local), pause a geração. Apresente um resumo sucinto do que foi feito e os resultados dos testes. Aguarde o comando "prosseguir" do usuário para carregar o `prd.json` e iniciar o próximo loop.
