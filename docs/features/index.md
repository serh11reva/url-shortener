# Features Overview

## Core Domain

| Feature | Description | Doc |
|--------|-------------|-----|
| URL Shortening | Create short URL from long URL; counter-based 7-char code; optional user-defined alias; optional idempotent create | [url-shortening.md](./url-shortening.md) |
| Redirects | Visiting short URL redirects to original; target <100ms; 404 for expired/missing | [redirects.md](./redirects.md) |
| Expiration | Optional expiration date; expired → 404; delete if not accessed for 1 month | [expiration.md](./expiration.md) |
| Analytics | Click count and last-accessed timestamp; eventual consistency; async updates | [analytics.md](./analytics.md) |

## Cross-Cutting

| Feature | Description | Doc |
|--------|-------------|-----|
| Security | Input validation; abuse protection; rate limiting per IP; basic throttling | [security.md](./security.md) |
| Observability | Logging; metrics; tracing (OpenTelemetry / Aspire) | [observability.md](./observability.md) |

## Documentation Map

- **Solution & NFRs:** [../solution.md](../solution.md)
- **Architecture:** [../architecture.md](../architecture.md)
- **Decisions (ADRs):** [../decisions.md](../decisions.md)
- **Tasks:** [../tasks.md](../tasks.md)
- **Definition of Done:** [../definition-of-done.md](../definition-of-done.md)
- **Agent personas:** [../../AGENTS.md](../../AGENTS.md)
