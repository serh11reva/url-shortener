# Solution Overview: URL Shortener

## Purpose

This system is ann URL shortener system designed to demonstrate senior-level software engineering and system design capabilities. It showcases high-performance reads, scalable writes, asynchronous processing, and robust cloud-native infrastructure for technical evaluators.

## Core Workflows

1. **Shorten URL:** A user submits a long URL (and optional custom alias). The system generates a unique short code (up to 7 characters) from a counter (Base62), stores the mapping in Cosmos DB, and caches it in Redis. Creation can be made idempotent (same long URL + alias returns existing short URL).
2. **Redirect:** A user accesses the short URL. The system checks Redis first. If found, it redirects within the target latency (<100ms). If not, it queries Cosmos DB and redirects **without** populating Redis on that path (read-through). Expired or missing links return 404.
3. **Expiration & Cleanup:** Optional per-link expiration date; expired links return 404. Links not accessed for one month are deleted via Cosmos DB TTL and/or a scheduled Azure Functions cleanup worker (timer trigger).
4. **Analytics:** Redirection events trigger asynchronous updates (e.g., channel/queue) to record click count and last-accessed timestamp in Cosmos DB, keeping the redirect path fast and accepting eventual consistency for analytics.

## Tech Stack

| Area | Choice |
|------|--------|
| Backend | .NET 10 Minimal API |
| Frontend | Vue 3 (Vite, TypeScript) |
| Database | Azure Cosmos DB (NoSQL API) |
| Cache | Redis |
| Local orchestration | .NET Aspire |
| Background jobs | Azure Functions (timer-trigger, isolated worker) |
| Deployment | Docker Compose; each service has its own Dockerfile |
| IaC | Modular Azure Bicep (target: Azure) |
| CI/CD | Pipelines for build, test, Docker images, Bicep deployment, app deployment |

## Repository & Code Conventions

- **Code rules:** The repository uses a root [.editorconfig](../.editorconfig) for formatting, style, and naming. All code must conform to it; agents should follow it when generating or editing code.
- **Central Package Management:** The solution uses Central Package Management (CPM). All .NET package versions are defined in the root [Directory.Packages.props](../Directory.Packages.props) file. Project files reference packages without a `Version` attribute (e.g., `<PackageReference Include="MediatR" />`). Do not add or change versions in individual `.csproj` files.

## Non-Functional Requirements

- **High read performance:** Redirect path optimized for <100ms (Redis cache-aside).
- **Idempotent creation (optional):** Same long URL (and optional alias) can return the same short URL when enabled.
- **Resilient to collisions:** Counter-based short codes; no hash collisions. Alias uniqueness enforced at creation.
- **Safe against abuse:** Basic rate limiting and throttling (e.g., per IP).

## Testing Strategy

- **Unit tests:** Domain logic, validation, short-code generation, handlers.
- **Integration tests:** API + Cosmos DB + Redis (or emulators); redirect and create flows.
- **End-to-End (E2E) tests:** Full user journey (create short URL, redirect, expired link 404) against running stack.

## Observability

- **Logging:** Structured logs; no silent failures; all exceptions logged.
- **Metrics:** Request counts, latency, cache hit/miss, error rates.
- **Tracing:** Distributed tracing (e.g., OpenTelemetry via Aspire); trace redirect and create flows.

## Security

- **Input validation:** URL format and length; short code and alias format and length.
- **Abuse protection:** Rate limiting per IP; basic throttling.
- **Consistent errors:** No sensitive data in responses; use RFC 7807 ProblemDetails.

## Error Handling

- **Consistent format:** RFC 7807 ProblemDetails for all API errors.
- **HTTP status codes:** 400 validation, 404 not found/expired, 429 rate limited, 500 with no silent failures.
- **No silent failures:** All exceptions logged; errors returned or rethrown as appropriate.

## Scalability & Performance

- **Read optimization:** Redis cache-aside for redirects (reads only on redirect path; create may prime Redis).
- **Write strategy:** Accept eventual consistency for analytics; keep redirect path write-free.
- **Horizontal scaling:** Stateless API; counter allocation strategy for horizontal scaling of short-code generation.

## Health Checks

- **Liveness:** API process is running.
- **Readiness:** Dependencies (Cosmos DB, Redis) are reachable; use standard ASP.NET Core health checks and Aspire.

## Definition of Done

See [Definition of Done](./definition-of-done.md). All work items must satisfy the DoD before being considered complete.

## Documentation Map for Agents

- **Architecture & structure:** [architecture.md](./architecture.md)
- **Features (detailed):** [features/index.md](./features/index.md) and linked feature docs
- **Decisions:** [decisions.md](./decisions.md)
- **Work items:** [tasks.md](./tasks.md)
- **Completion criteria:** [definition-of-done.md](./definition-of-done.md)
- **Agent personas:** [../AGENTS.md](../AGENTS.md)
