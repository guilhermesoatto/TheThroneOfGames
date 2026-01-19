# Refactoring & Fixes — Context and Rationale

This document explains the recent refactor-style fixes applied to get the solution building and tests passing, and records recommendations and technical debt for future maintainability.

## What changed

- The DI registration in `TheThroneOfGames.API/Program.cs` was ambiguous because two assemblies define `UsuarioAtivadoEventHandler` with the same type name. The subscription lines were changed to explicitly reference both handler types:
  - `GameStore.Catalogo.Application.EventHandlers.UsuarioAtivadoEventHandler`
  - `GameStore.Usuarios.Application.EventHandlers.UsuarioAtivadoEventHandler`

- Tests referenced concrete validator classes (e.g. `CreateUserCommandValidator`) and object initializers on commands, but the application uses positional record constructors and a centralized static `UsuarioValidators` implementation. To reconcile this:
  - Added small wrapper validator classes in `GameStore.Usuarios.Application.Validators.WrapperValidators.cs` that delegate to `UsuarioValidators`. These allow tests to instantiate `CreateUserCommandValidator` etc. without duplicating validation logic.

- The validator result shape was standardized inside the `GameStore.Usuarios` context to use `ValidationError` (with `PropertyName` and `ErrorMessage`) instead of plain strings. Tests and handlers were updated as follows:
  - Unit tests in `GameStore.Usuarios.Tests` were updated to construct commands using the positional constructors and to assert on `ValidationError.PropertyName` or `ValidationError.ErrorMessage` as appropriate.
  - Command handlers convert `ValidationError` to `List<string>` when returning `CommandResult.Errors` so external callers still receive a list of messages.

- Minor DTO and mapper changes were applied to align code with test expectations (for example, `GameDTO.Description` and mapper timestamp/availability fields). The domain event `UsuarioAtivadoEvent` was extended with small aliases and a parameterless constructor to maintain compatibility with test expectations.

## Why these changes

- Purpose: Rapidly restore a green build and test suite while keeping changes minimal and localized.
- The repository had drift between tests and current code (different shapes for records and different validation return types). This can happen during refactors when tests are not updated in lockstep.
- The DI ambiguity is due to duplicated type names across bounded contexts — this is a symptom of duplicate definitions rather than an issue with DI itself.

## Risks and trade-offs

- Wrapper validators are a pragmatic compatibility shim. They intentionally duplicate type names expected by tests, delegating to the canonical validation logic. They should be removed in a follow-up once tests and the public API are aligned to a single canonical shape.
- Converting ValidationResult to include `ValidationError` objects may affect callers expecting plain string lists; handlers currently convert errors back to strings for `CommandResult`, but external code or third-party consumers may need adjustments.

## Recommended follow-ups (technical debt remediation)

1. Unify CQRS interfaces
   - Remove duplicate CQRS interface definitions across projects and reference the canonical `GameStore.CQRS.Abstractions` from all bounded-context projects. This will eliminate the need for fully-qualified generic type registrations in the composition root.

2. Consolidate ValidationResult types
   - Decide on a single `ValidationResult`/`ValidationError` contract used across the solution. Either:
     - Adopt `ValidationError { PropertyName, ErrorMessage }` universally and update `CommandResult` to carry error objects, or
     - Keep `CommandResult.Errors` as `List<string>` and adapt validators to return `List<string>` consistently. Be explicit and document the contract.

3. Remove wrapper validators
   - After tests and callers are updated to use the canonical API, delete `WrapperValidators.cs` and update references.

4. Move/compile user query handler types
   - Some query handler source files live in folders not compiled into the projects referenced by the API. Decide whether to:
     - Move the sources into the `GameStore.Usuarios` project (if they belong there), or
     - Add a small assembly that contains the public query handler types and reference it from the API.

5. Password validation clarity
   - The tests previously asserted on the number of password-related errors. Tests should instead assert on specific required rules (e.g., missing uppercase/lowercase/number/length). Adjust tests to reflect policy rather than counts.

6. Revisit event subscriptions
   - Consider centralizing event subscriptions or using a discovery/DI-based registration mechanism instead of manual `Subscribe<T>` calls, which require concrete handler instantiation and can cause ambiguity.

## Where to find the edits

- `TheThroneOfGames.API/Program.cs` — explicit event handler subscriptions
- `GameStore.Usuarios/Application/Validators/WrapperValidators.cs` — new wrapper validators
- `GameStore.Usuarios.Application.Validators` — validator shape updates
- `GameStore.Usuarios.Tests` — tests updated to match current shapes
- `GameStore.Catalogo.Application.DTOs` & `GameStore.Catalogo.Application.Mappers` — DTO/mapping fixes

## How to revert or continue

- To revert the compatibility shims quickly: remove `WrapperValidators.cs` and re-run tests — they will fail if tests were not updated to canonical API shapes.
- To continue the refactor: pick follow-up item #1 (unify CQRS interfaces) and #2 (consolidate ValidationResult) to reduce duplication and make DI composition simpler.

---
Generated on: 2025-12-04
