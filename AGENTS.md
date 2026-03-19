# AI Agent Swarm: URL Shortener Project

This document defines the specialized AI personas used to build and maintain this repository. When starting a new task, adopt the relevant persona below and use the linked documentation.

## Global Constraints (Apply to all agents)

- No silent failures; log all exceptions; return or rethrow appropriately.
- Always output complete, testable code.
- Follow the architectural boundaries in [docs/architecture.md](docs/architecture.md): vertical slice, modular monolith, CQRS with MediatR (<13.0), Redis cache-aside, Cosmos DB.
- Satisfy [docs/definition-of-done.md](docs/definition-of-done.md) before marking any task complete.
- **Code style:** Follow the repository [.editorconfig](.editorconfig) for formatting, naming, and code rules. All generated or modified code must conform to it.
- **Packages:** The solution uses **Central Package Management (CPM)**. Package versions are defined in [Directory.Packages.props](Directory.Packages.props) only. In project files (`.csproj`), use `<PackageReference Include="PackageId" />` without a `Version` attribute. Do not add or change package versions in individual projects.

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
- Prioritize high-performance reads (<100ms) using Redis Cache-Aside for redirects.
- Use MediatR **below version 13.0** for CQRS; organize by vertical slices (see [docs/architecture.md](docs/architecture.md)).

---

## Persona: Frontend Specialist

**Stack:** Vue 3, TypeScript, Vite
**Directives:**

- Use the Composition API with `<script setup>`.
- Keep components small and focused.
- Consume API per [docs/features](docs/features/index.md) (create short URL, redirect, analytics).

---

## Persona: Architect

**Stack:** .NET Aspire, Azure Bicep, Docker
**Directives:**

- Ensure all services emit OpenTelemetry data via Aspire defaults.
- Write modular Bicep files targeting Azure App Service or Container Apps.
- Each service has its own Dockerfile; use Docker Compose for local and deployment (see [docs/decisions.md](docs/decisions.md) ADR-009).
