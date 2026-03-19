# System Architecture

## Architectural Style

- **Modular monolith:** Single deployable API with clear internal modules (e.g., Shortening, Redirect, Analytics).
- **Vertical slice architecture:** Organize by feature/use case (e.g., “CreateShortUrl”, “RedirectByCode”) rather than by technical layer. Each slice includes its own handlers, validation, and persistence concerns.
- **Domain-driven design (DDD):** Bounded contexts aligned to features; rich domain where it adds value (e.g., ShortUrl, Redirect, Analytics); repositories and application services at boundaries.
- **CQRS:** Commands (e.g., CreateShortUrl, RecordClick) and Queries (e.g., GetRedirectTarget) separated. Use a free, in-process mediator for CQRS (MediatR version **below 13.0**).
- **API style:** The backend is implemented as a **Minimal API** (ASP.NET Core minimal hosting / `Map*` endpoints), not controller-based MVC. Handlers are invoked via MediatR from minimal API route handlers.

All agents must respect these boundaries and the component diagram below.

## Component Diagram

```mermaid
graph TD
    Client[Client Browser / Vue 3 App] -->|HTTP POST /api/urls| API[.NET 10 Minimal API]
    Client -->|HTTP GET /{shortCode}| API

    API -->|Cache-Aside Read/Write| Redis[(Redis)]
    API -->|Read/Write| Cosmos[(Cosmos DB)]

    API -.->|Async / In-Process Channel| Analytics[Analytics Handler]
    Analytics -->|Batch / Eventual Writes| Cosmos
```

## Deployment

- **Local:** .NET Aspire orchestrates API, Redis, Cosmos DB (or emulators).
- **Containers:** Docker Compose for multi-service run; each service (API, frontend if served separately) has its own Dockerfile.
- **Cloud:** Azure; infrastructure defined with modular Bicep (e.g., App Service or Container Apps, Cosmos DB, Redis, resource groups).

## Observability

- All services emit **OpenTelemetry** (traces, metrics) via Aspire defaults where applicable.
- Health endpoints: liveness and readiness (including Cosmos DB and Redis).

## Documentation Map

- Features and rules: [features/index.md](./features/index.md)
- Decisions (ADRs): [decisions.md](./decisions.md)
- Solution goals and NFRs: [solution.md](./solution.md)
