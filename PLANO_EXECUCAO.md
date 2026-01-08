# Plano de Execução - The Throne of Games

## Status Atual: 37/48 testes passando (77%)

## Meta: 100% de Sucesso em Todas as Etapas

---

## FASE 1: Testes Locais ✅ PRIORIDADE MÁXIMA

### Objetivo: 100% dos Testes Passando Localmente

#### 1.1 Testes de Integração
- **Configuração**: Usar SQL Server do container Docker
- **Connection String**: Mesma usada pela aplicação
- **Migrations**: Executar antes dos testes para garantir schema atualizado
- **Status**: 🔴 Em progresso - 37/48 (77%)
- **Bloqueio Atual**: 11 testes falhando relacionados a registro/login de usuários

#### 1.2 Testes Unitários
- **Configuração**: Usar Moq para mocks de dependências
- **Banco de Dados**: Não usar banco real, apenas mocks
- **Status**: ⚠️ A verificar

#### 1.3 Checklist Fase 1
- [ ] Reverter configuração SQLite dos testes
- [ ] Configurar testes de integração para usar SQL Server do container
- [ ] Garantir que migrations rodem automaticamente antes dos testes
- [ ] Verificar se todas as tabelas necessárias existem no banco
- [ ] Resolver erro "InternalServerError" no endpoint `/api/Usuario/pre-register`
- [ ] Resolver erro "Admin login failed" nos testes de admin
- [ ] Atingir 48/48 testes passando (100%)

---

## FASE 2: Estrutura de Banco de Dados para Microserviços

### Objetivo: Migrations Funcionando para Cada Contexto

#### 2.1 Bounded Contexts
- **GameStore.Usuarios**: UsuariosDbContext
- **GameStore.Catalogo**: CatalogoDbContext
- **GameStore.Vendas**: VendasDbContext
- **TheThroneOfGames.Infrastructure**: MainDbContext

#### 2.2 Estratégia de Migrations
- [ ] Migrations separadas para cada contexto
- [ ] Scripts de inicialização de banco
- [ ] Seed data para ambientes de desenvolvimento/teste
- [ ] Versionamento de schema

#### 2.3 Checklist Fase 2
- [ ] Criar migrations para UsuariosDbContext
- [ ] Criar migrations para CatalogoDbContext
- [ ] Criar migrations para VendasDbContext
- [ ] Criar migrations para MainDbContext
- [ ] Script único para executar todas as migrations
- [ ] Validar integridade referencial entre contextos
- [ ] Documentar processo de atualização de schema

---

## FASE 3: Performance Testing no Google Cloud

### Objetivo: Avaliar Capacidade e Auto-scaling

#### 3.1 Métricas a Avaliar
- Requisições por segundo (RPS)
- Usuários simultâneos suportados
- Latência média/p95/p99
- Ponto de quebra antes do auto-scaling
- Comportamento do auto-scaling

#### 3.2 Ferramentas
- K6 ou Artillery para load testing
- Google Cloud Monitoring
- Análise de logs e métricas

#### 3.3 Checklist Fase 3
- [ ] Deployment bem-sucedido no GKE
- [ ] Script de performance configurado
- [ ] Baseline de performance estabelecido
- [ ] Testes de carga progressiva
- [ ] Análise de bottlenecks
- [ ] Otimizações implementadas
- [ ] Documentação de resultados

---

## CI/CD - Pós Fase 1

### Checklist CI/CD
- [ ] GitHub Actions configurado
- [ ] Testes automáticos no pipeline
- [ ] Build de imagens Docker
- [ ] Deploy automático para GKE
- [ ] Rollback automático em caso de falha

---

## Notas Importantes

### Arquitetura
- 4 DbContexts separados (bounded contexts pattern)
- Preparação para futura migração para microservices
- Cada contexto pode se tornar um serviço independente

### Decisões Técnicas
- **Testes de Integração**: SQL Server (mesmo do container)
- **Testes Unitários**: Moq (sem banco real)
- **Ambiente**: .NET 9.0 com EF Core 9.0

### Princípios
1. Não tentar resolver sem entender o problema
2. Validar hipóteses antes de implementar
3. Foco em uma fase por vez
4. 100% de sucesso em cada etapa antes de avançar

---

## Timeline

- **Fase 1**: AGORA - Testes 100% funcionando
- **Fase 2**: Após Fase 1 - Migrations e estrutura DB
- **Fase 3**: Após Fase 2 - Performance testing no GCP
- **CI/CD**: Após validação da Fase 1

---

**Última Atualização**: 08/01/2026
**Status Global**: 🔴 Fase 1 em progresso
