# Fix — Admin user-management sem endpoint HTTP (gap Fase 1)

> Encontrado numa auditoria de regras de negócio contra `docs/Objectives/TC NETT - Fase 1.pdf`
> em 2026-07-31. Implementado nesta mesma sessão em seguida — ver commits subsequentes.
> Mantido aqui para rastreabilidade do achado original.

Estou no repositório TheThroneOfGames (branch release/fase-4-kubernetes). Uma auditoria contra
docs/Objectives/TC NETT - Fase 1.pdf encontrou um gap: o requisito "Administrador... administra
usuários" nunca foi exposto por HTTP em GameStore.Usuarios.API, embora toda a lógica já exista.
NÃO mexa em GameStore.Partidas nem em nada de deploy/k8s/pipeline — é um fix isolado.

## Gap — Admin user-management sem endpoint HTTP

Evidências:
- api-gateway/nginx.conf já roteia `/api/admin/user-management/` para `usuarios-api:80/api/
  admin/user-management/` (linha ~32-34) — a infra já espera esse endpoint existir.
- GameStore.Usuarios.API não tem nenhum arquivo mencionando "admin" (grep confirmado) — só
  existe GameStore.Usuarios.API/Controllers/UsuarioController.cs, com as rotas públicas de
  self-service (pre-register/activate/login/profile/possui-jogo).
- A lógica de aplicação já existe e está pronta, só falta o Controller:
  - GameStore.Usuarios/Application/Services/UsuarioService.cs: `GetAllUsersAsync`,
    `GetUserByIdAsync`, `UpdateUserRoleAsync`, `DisableUserAsync`, `EnableUserAsync`
  - GameStore.Usuarios/Application/Handlers/UsuarioCommandHandlers.cs: `CreateUserCommandHandler`,
    `ChangeUserRoleCommandHandler` (CQRS, via ICommandHandler<T>)
  - GameStore.Usuarios/Application/Validators/UsuarioValidators.cs já valida CreateUserCommand
    e ChangeUserRoleCommand (email via regex, role só "User"/"Admin")

Fix: criar `GameStore.Usuarios.API/Controllers/Admin/UserManagementController.cs`, seguindo
EXATAMENTE o padrão já usado em GameStore.Catalogo.API/Controllers/Admin/GameController.cs +
GameStore.Catalogo.API/Controllers/Base/AdminControllerBase.cs (leia os dois arquivos antes de
começar — é o template). Nesse padrão: um `AdminControllerBase` abstrato com
`[ApiController] [Route("api/admin/[controller]")] [Authorize(Roles = "Admin")]` +
`HandleError(Exception)`/`NotFoundById<T>(Guid)` auxiliares, e o controller concreto injeta
`ICommandHandler<TCommand>`/`IQueryHandler<TQuery,TResult>` no construtor (DI já deve estar
registrado no Program.cs do Usuarios.API — confirme; se os handlers não estiverem no DI
container, registre-os igual aos de Catálogo).

IMPORTANTE sobre a rota: crie (ou reutilize, se decidir criar um AdminControllerBase próprio
para Usuarios) com `[Route("api/admin/user-management")]` EXPLÍCITO na classe — o token
`[controller]` do padrão de Catálogo geraria "UserManagement" (PascalCase), não
"user-management" (kebab-case), e não bateria com o que o nginx já espera. Não existe nenhum
transformador de rota kebab-case configurado no projeto (confirmado) — tem que ser explícito.

Endpoints a expor (todos atrás de `[Authorize(Roles = "Admin")]`, herdado do AdminControllerBase):
- `GET  /api/admin/user-management`           → lista todos os usuários (GetAllUsersAsync)
- `GET  /api/admin/user-management/{id}`      → busca usuário por ID (GetUserByIdAsync)
- `POST /api/admin/user-management`           → cria usuário via CreateUserCommandHandler
- `PATCH /api/admin/user-management/{id}/role` (ou similar) → troca role via
  ChangeUserRoleCommandHandler (o command atual usa email, não id — avalie se ajusta o
  command pra aceitar id, ou mantém email no body; documente a decisão)
- `POST /api/admin/user-management/{id}/disable` → DisableUserAsync
- `POST /api/admin/user-management/{id}/enable`  → EnableUserAsync

Cuidado: não deixe um admin desativar a própria conta nem a última conta Admin ativa do
sistema (trava simples, tipo `if (usuarios ativos com role Admin) <= 1 então bloqueia`) — isso
não está no PDF da Fase 1 explicitamente, mas é uma trava de bom senso pra não travar o
próprio sistema; comente no código o porquê caso implemente.

## Instruções gerais

- Siga os padrões já existentes (CQRS, DTOs, mappers) — não introduza abstrações novas.
- Adicione testes em GameStore.Usuarios.Tests cobrindo cada endpoint novo (sucesso, 401/403
  sem role Admin, 404 usuário inexistente).
- Rode `dotnet test` para GameStore.Usuarios.Tests antes de considerar concluído.
- Depois de implementar, teste manualmente via Swagger (GameStore.Usuarios.API/swagger) com um
  usuário Admin real (o fluxo de registro/ativação/login já funciona — use-o pra gerar um token
  Admin) antes de dar como pronto.
