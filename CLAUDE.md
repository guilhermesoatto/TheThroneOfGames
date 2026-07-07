# TheThroneOfGames — Governança IA

**Suitcase injetada em:** 2026-07-04
**Stack:** dotnet
**Framework:** ia-arquiteto-hexagon-pattern (DDD + Hexagonal + K8s)

## Ordem de Leitura Obrigatória (Bootstrap)

`
1. docs/ai/knowledge/CONTEXT.md       — estado atual do projeto
2. docs/ai/architecture.md            — topologia e contratos
3. docs/ai/skills/agent-laws.md       — leis imutáveis e fronteiras de autonomia
4. docs/ai/skills/dotnet-clean-arch.md — padrões de código (.NET)
5. docs/ai/tasks/prd-sprint-00-migration.json — PRD ativo
`

## Roteamento Rápido

| Necessidade                  | Arquivo                                          |
|------------------------------|--------------------------------------------------|
| Estado do projeto            | docs/ai/knowledge/CONTEXT.md                   |
| Padrão de código .NET        | docs/ai/skills/dotnet-clean-arch.md            |
| Regras de persistência       | docs/ai/skills/efcore-postgres.md              |
| Testes                       | docs/ai/skills/xunit-native-testing.md         |
| Eventos                      | docs/ai/skills/event-versioning.md             |
| Observabilidade              | docs/ai/skills/opentelemetry-logs.md           |
| Leis do agente               | docs/ai/skills/agent-laws.md                   |
| PRD ativo                    | docs/ai/tasks/prd-sprint-00-migration.json     |
| Validar PRD                  | 
ode tools/validate-prd.js docs/ai/tasks/prd-sprint-00-migration.json |

## Bounded Contexts (TheThroneOfGames)

| Microsserviço  | Legado origem              | Namespace K8s     |
|----------------|----------------------------|-------------------|
| identity-svc   | GameStore.Usuarios         | identity          |
| catalog-svc    | GameStore.Catalogo         | catalog           |
| payments-svc   | GameStore.Vendas           | payments          |

## Leis Invioláveis

- NUNCA altere arquivos em GameStore.*/, TheThroneOfGames.*/, PlataformaJogos.* sem ler o PRD ativo.
- NUNCA crie chamadas HTTP síncronas entre Bounded Contexts — use eventos via RabbitMQ.
- NUNCA avance de fase no PRD sem aprovação do Domain Expert.
- Leia docs/ai/skills/agent-laws.md §2 antes de qualquer tarefa de implementação.