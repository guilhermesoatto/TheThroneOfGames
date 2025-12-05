Fix: Resolve DI ambiguity, align validators/tests, and restore build/tests

Summary:
- Fixed ambiguous `UsuarioAtivadoEventHandler` subscription in `TheThroneOfGames.API/Program.cs` by explicitly subscribing both the `Catalogo` and `Usuarios` event handlers.
- Added small validator wrapper classes (`GameStore.Usuarios.Application.Validators.WrapperValidators.cs`) so test code can instantiate concrete validator types while keeping validation logic centralized in `UsuarioValidators`.
- Updated `GameStore.Usuarios.Application.Validators` to return `ValidationError` objects (PropertyName + ErrorMessage) and adjusted handlers to convert those into `CommandResult.Errors` string lists when returning command results.
- Aligned unit tests in `GameStore.Usuarios.Tests` to match the current command shapes (positional record constructors) and the validator return shape (check `ValidationError.ErrorMessage`).
- Minor mapper/event/DTO alignments to match test expectations (e.g. `GameDTO.Description`, `UsuarioAtivadoEvent` aliases) and keep backward compatibility.

Why:
- The repository had drift between tests and current code (positional records vs. object initializer usage and differing validation return shapes), which caused compile and test failures and ambiguous type references in DI registrations.
- These changes are minimal, focused fixes to restore green build & tests and to make behavior explicit while preserving existing business logic.

Notes:
- This commit contains small compatibility helpers (wrapper validators). These are temporary, pragmatic fixes to get tests to compile and pass. See `docs/REFACTORING_CHANGELOG.md` for long-term recommendations (unify CQRS interfaces, consolidate ValidationResult type, and remove wrapper classes by updating tests/code to a single canonical API).

Files changed/added (high level):
- TheThroneOfGames.API/Program.cs (explicit event subscriptions)
- GameStore.Usuarios/Application/Validators/WrapperValidators.cs (new)
- GameStore.Usuarios.Application.Validators/* (validator shape updates)
- GameStore.Usuarios.Tests/* (test constructor and assertion updates)
- GameStore.Catalogo.Application.DTOs/GameDTO.cs (added Description)
- GameStore.Catalogo.Application.Mappers/GameMapper.cs (mapping updates)

Risk & Follow-ups:
- Remove duplicate CQRS interface definitions and make all projects reference `GameStore.CQRS.Abstractions` consistently.
- Unify ValidationResult into a single canonical type across the solution; remove local ValidationError wrappers once tests and handlers agree on the representation.
- Re-evaluate password validation policy and adjust tests to assert on domain-correct behavior rather than count-based expectations.

Signed-off-by: Automated refactor bot
