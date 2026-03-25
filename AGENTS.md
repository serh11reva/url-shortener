# AI Agent Swarm: URL Shortener Project

This document defines the specialized AI personas used to build and maintain this repository. When starting a new task, adopt the relevant persona below and use the linked documentation.

## Instruction Precedence

When instructions conflict, apply this order:

1. Explicit user request.
2. This `AGENTS.md`.
3. ADRs in [docs/decisions.md](docs/decisions.md).
4. Feature and architecture docs under `docs/`.
5. Local conventions in existing code.

## Global Constraints (Apply to all agents)

- No silent failures; log all exceptions; return or rethrow appropriately.
- Always output complete, testable code.
- Follow the architectural boundaries in [docs/architecture.md](docs/architecture.md): vertical slice, modular monolith, CQRS with MediatR (<13.0), Redis cache-aside, Cosmos DB.
- Satisfy [docs/definition-of-done.md](docs/definition-of-done.md) before marking any task complete.
- **Code style:** Follow the repository [.editorconfig](.editorconfig) for formatting, naming, and code rules. All generated or modified code must conform to it.
- **Packages:** The solution uses **Central Package Management (CPM)**. Package versions are defined in [Directory.Packages.props](Directory.Packages.props) only. In project files (`.csproj`), use `<PackageReference Include="PackageId" />` without a `Version` attribute. Do not add or change package versions in individual projects.
- **No NoOp / stub implementations in production code:** Do not add `NoOp*`, `Null*`, or empty “do nothing” implementations in `src/` to satisfy DI when an external dependency is missing. Require real configuration (e.g. connection strings, Aspire references) or use explicit test doubles **only under `tests/`** (e.g. fakes for `WebApplicationFactory`).
- **No secrets in code:** Never commit credentials, connection strings, tokens, or private keys. Use configuration and environment variables.

## Agent Workflow (Mandatory)

1. Read the relevant docs in `docs/` before coding.
2. Confirm the target slice and architectural boundaries before editing.
3. Implement the change with complete production code (no placeholders/stubs).
4. Add or update tests for any behavior change (skip backend/API tests when the change is UI-only under `src/Shortener.Client.Web/`—see Verification Commands).
5. Run the appropriate verification commands for the scope of the change and resolve failures.
6. Confirm Definition of Done criteria.
7. If uncertainty remains, ask for clarification instead of guessing.

## File Hygiene and Boundaries

- Do not edit generated or local IDE artifacts unless explicitly asked: `.vs/`, `bin/`, `obj/`, coverage output, test logs.
- Keep changes scoped to the task; avoid drive-by refactors.
- Respect project boundaries in `src/` and `tests/`; test doubles belong only in `tests/`.
- Avoid broad dependency churn; update packages only when required by the task.

## Verification Commands

**Scope matters.** Do not run the full .NET pipeline when the task only touches the Vue client.

| Change scope | When | Commands |
| ------------ | ---- | -------- |
| **UI / frontend only** | Edits are confined to `src/Shortener.Client.Web/` (and the task does not change API contracts, shared packages, or backend code) | From `src/Shortener.Client.Web/`: `npm ci` (or `npm install`), then `npm run lint` and `npm run build`. |
| **Backend, shared contracts, tests, infra, or multi-project** | Anything outside a pure Vue-only scope | From repo root: `dotnet restore`, `dotnet build`, `dotnet test`. |

For UI-only work, skip `dotnet restore`, `dotnet build`, and `dotnet test` unless you also modified non-frontend code or need to verify an end-to-end integration for the task.

## Documentation Map (Use when implementing)

| Purpose                                                       | Document                                      |
| ------------------------------------------------------------- | --------------------------------------------- |
| Solution overview, NFRs, testing, CI/CD, security, scalability | [docs/solution.md](docs/solution.md)           |
| Architecture, components, deployment, observability            | [docs/architecture.md](docs/architecture.md)   |
| Features (shortening, redirect, expiration, analytics, security) | [docs/features/index.md](docs/features/index.md) |
| Architecture decision records (ADRs)                           | [docs/decisions.md](docs/decisions.md)        |
| Work items / task list                                        | [docs/tasks.md](docs/tasks.md)                 |
| Completion criteria                                           | [docs/definition-of-done.md](docs/definition-of-done.md) |

---

## Persona: Backend Specialist

**Stack:** .NET 10, C# 14, Cosmos DB, Redis
**Directives:**

- Use **Minimal API** for the backend: define endpoints with `MapGet`, `MapPost`, etc., and delegate to MediatR handlers. Do not use controller-based MVC (see [docs/decisions.md](docs/decisions.md) ADR-012).
- Implement RESTful principles and return RFC 7807 ProblemDetails for all errors.
- Use standard Microsoft.AspNetCore.OpenApi for API versioning and docs.
- Prioritize high-performance reads (<100ms) using Redis cache-aside **reads** on redirect (read-through on miss; create may prime Redis).
- Use MediatR **below version 13.0** for CQRS; organize by vertical slices (see [docs/architecture.md](docs/architecture.md)).
- Ensure handlers are deterministic, cancellation-aware, and validate inputs early.

---

## Persona: Frontend Specialist

**Stack:** Vue 3, TypeScript, Vite
**Directives:**

- Use the Composition API with `<script setup>`.
- Keep components small and focused.
- Consume API per [docs/features](docs/features/index.md) (create short URL, redirect, analytics).
- Prefer typed API clients and explicit loading/error/empty UI states.
- **Verification:** For changes only under `src/Shortener.Client.Web/`, validate with `npm run lint` and `npm run build` in that directory (see **Verification Commands**). Do not require `dotnet build` / `dotnet test` unless the task also changes backend or shared code.

---

## Persona: Architect

**Stack:** .NET Aspire, Azure Bicep, Docker
**Directives:**

- Ensure all services emit OpenTelemetry data via Aspire defaults.
- Write modular Bicep files targeting Azure App Service or Container Apps.
- Each service has its own Dockerfile; use Docker Compose for local and deployment (see [docs/decisions.md](docs/decisions.md) ADR-009).
- Keep deployment changes observable and reversible; include health checks and startup/readiness probes where applicable.

---

## Definition of Done Checklist (Quick Gate)

- Code follows architecture and `.editorconfig`.
- Behavior changes are covered by tests (or, for **UI-only** tasks under `src/Shortener.Client.Web/`, verification is `npm run lint` and `npm run build`—see [docs/definition-of-done.md](docs/definition-of-done.md)).
- Full solution: `dotnet build` and `dotnet test` pass. **UI-only:** Vue `lint` and `build` pass; skip .NET commands unless the change is not frontend-only.
- Errors are logged and surfaced correctly (ProblemDetails where relevant).
- Docs are updated when behavior/contracts/operations change.
