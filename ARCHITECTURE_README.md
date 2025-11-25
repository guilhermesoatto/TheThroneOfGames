# Arquitetura proposta — Bounded Contexts

Objetivo: separar o monólito em contextos limitados para facilitar evolução e futura migração a microservices.

Contextos iniciais criados neste repositório:
- GameStore.Catalogo
- GameStore.Usuarios
- GameStore.Vendas

Próximos passos detalhados:
1. Criar csproj esqueleto para cada contexto e adicioná-los à solution (.sln).
2. Mapear classes atuais de `TheThroneOfGames.Domain`, `Application` e `Infrastructure` para os contexts apropriados.
3. Implementar contracts/DTOs e handlers independentes; usar Events para comunicação entre contexts.
4. Adicionar testes unitários por contexto e garantir build verde.
