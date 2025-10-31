# Finalization Plan — TheThroneOfGames (Phase 1)

This document is a step-by-step guide for an intern to finish the project and reach the objectives listed in `objetivo1.md`.

Purpose
-------
Provide a clear checklist, technical instructions, rationale for the chosen approaches, and concrete tasks that complete the MVP (user registration, activation, authentication, admin features, tests and deliverables).

How to use this document
------------------------
- Follow the ordered TODO list below.
- Each TODO contains file(s) to edit, a short implementation recipe, and tests to add or update.
- Use the commands shown to run, build and test the project locally.

Top-level TODO (summary)
------------------------
1. Email activation integration (current priority)
2. Admin functionality (games / promotions / user management)
3. End-to-end tests and extra coverage
4. README and delivery report
5. Production hardening and CI

Detailed tasks
--------------

1) Email activation integration (ETA: 2-4 hours)

Goal
: When a user pre-registers, the system persists the user and sends an activation e-mail containing an activation link with the activation token. Activation link calls `POST /api/Usuario/activate?activationToken={token}` to activate the user.

Why this approach
: Keeping activation token generation server-side and sending a link by e-mail is a common, secure pattern. We write e-mails to an Outbox (file) in development so tests can assert delivery without requiring a real SMTP server.

Files to change
:
- `TheThroneOfGames.Application/Interface/IUsuarioService.cs` — change PreRegisterUserAsync to return the activation token (Task<string>).
- `TheThroneOfGames.Application/UsuarioService.cs` — after persisting user, return the generated activation token.
- `TheThroneOfGames.API/Controllers/UsuarioController.cs` — inject `EmailService` and call `SendEmailAsync` with activation link after PreRegisterUserAsync returns the token.
- `TheThroneOfGames.Infrastructure/ExternalServices/EmailService.cs` — implement file-based Outbox writer (create `Infrastructure/Outbox` and write .eml file with headers and body). This avoids external SMTP for tests.
- `TheThroneOfGames.API/Program.cs` — register `EmailService` in DI.

Tests
:
- Update unit tests that call `PreRegisterUserAsync` to accept/ignore returned token and assert it is not null/empty.
- Add an integration test that runs PreRegister flow and asserts an Outbox file was created and contains the activation token.

Acceptance criteria
:
- Pre-register endpoint returns 200 and an Outbox file with activation link exists.
- `ActivateUserAsync` uses the token to activate the user (existing tests cover this).

2) Admin functionality (ETA: 1–2 days)

Goal
: Implement endpoints and services for admins to create and manage games and promotions.

Why this approach
: Role-based authorization (`[Authorize(Roles = "Admin")]`) allows separating admin flows from user flows. Keep controllers small and delegate business rules to Application services.

Files to create/update
:
- `TheThroneOfGames.Domain/Entities/GameEntity.cs`, `Promotion.cs` (domain models already present)
- `TheThroneOfGames.Application/GameService.cs` and `PromotionService.cs`
- `TheThroneOfGames.Infrastructure/Repository/GameRepository.cs` using `BaseRepository`
- `TheThroneOfGames.API/Controllers/Admin/GameController.cs` with `[Authorize(Roles = "Admin")]`

Tests
:
- Unit tests for GameService and PromotionService
- Integration tests ensuring only admin tokens can access admin endpoints

3) End-to-end tests & coverage (ETA: 4–8 hours)

Goal
: Add tests that cover the activation flow, login, protected endpoints (user vs admin), and password edge cases.

Strategy
:
- Use the existing test project; add integration tests that mock repositories and use the API-level services.
- For E2E system tests, run the API in-memory with TestServer and use an ephemeral database (SQLite in-memory) to exercise flows.

4) README + Delivery Report (ETA: 1–2 hours)

Goal
: Provide clear instructions on how to run, test and deploy the project, and create the required delivery report.

Contents
:
- Project summary and objective
- Setup (dotnet SDK, SQL Server instance or Docker image)
- Running migrations (dotnet ef database update)
- Running API and tests
- How to produce the delivery report

5) Production hardening & CI (ETA: 1–2 days)

Tasks
:
- Move secrets to environment variables; update `Program.cs` to read from env in production.
- Add Serilog for structured logs (console + file), and health checks.
- Add a Dockerfile for the API.
- Add GitHub Actions for build, test and basic linting.

Coding conventions / quick rules for the intern
--------------------------------------------
- Keep services thin; business rules in Application layer. Controllers only map DTOs and call services.
- Use DI and prefer interfaces for testability.
- Write unit tests for business logic and at least one integration test for every user-visible flow.
- Avoid committing secrets. Use placeholders in `appsettings.json` and document how to set env vars.

Useful commands
---------------
- Run API (development):
  dotnet run --project TheThroneOfGames.API
- Run tests:
  dotnet test TheThroneOfGames.sln
- Build solution:
  dotnet build TheThroneOfGames.sln

Contact / Handover notes
------------------------
If anything is unclear, ask the project owner to explain the intended UX for activation emails and admin features. Keep changes small and test-driven.

---
Generated on: (auto)
