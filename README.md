# URL Shortener

A high-performance, cloud-native URL shortener built with .NET 10 and Vue 3. Shorten long URLs, redirect with sub-100ms latency, and track clicks with optional expiration and cleanup.

---

## Features

- **Shorten URLs** — Submit a long URL; get a unique short code (up to 7 characters, Base62). Optional custom alias and idempotent creation (same URL + alias returns existing short link).
- **Fast redirects** — Redis cache-aside keeps redirects under 100ms; fallback to Cosmos DB on cache miss. Expired or missing links return 404.
- **Expiration & cleanup** — Optional per-link expiration date; links not accessed for one month can be removed (Cosmos DB TTL plus scheduled cleanup via Azure Functions timer trigger).
- **Analytics** — Click count and last-accessed timestamp, updated asynchronously so the redirect path stays write-free and fast.
- **Abuse protection** — Rate limiting and throttling per IP; RFC 7807 ProblemDetails for consistent error responses.

---

## Tech Stack

| Layer         | Technology                                              |
| ------------- | ------------------------------------------------------- |
| Backend       | .NET 10, C# 14, Minimal API, CQRS with MediatR          |
| Frontend      | Vue 3, TypeScript, Vite                                 |
| Database      | Azure Cosmos DB (NoSQL)                                 |
| Cache         | Redis                                                   |
| Orchestration | .NET Aspire                                             |
| Deployment    | Docker Compose, Azure Bicep                             |

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Redis and Cosmos DB emulators or containers)

### Run locally with Aspire

From the repository root:

```bash
dotnet run --project src/Shortener.AppHost
```

Aspire starts the API, Redis, Cosmos DB emulator, Azure Storage emulator (Azurite), and the cleanup Azure Functions host (`Shortener.Host.Functions`). Open the Aspire dashboard URL shown in the console to inspect services and logs.

The cleanup function schedule is configured in `src/Shortener.Host.Functions/local.settings.json` using `CleanupSchedule` (development default: once per minute).

### Run with Docker Compose

When the stack is containerized (see [tasks](docs/tasks.md)):

```bash
docker compose up --build
```

### Environment variables

Connection strings and keys (Cosmos DB, Redis) are configured via Aspire or environment variables. For local development, use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or a `.env` file (see `.env.example` if present). Do not commit secrets.

---

## Project Structure

```text
src/
├── Shortener.AppHost/              # Aspire host (orchestrates API, Redis, Cosmos)
├── Shortener.Host.Api/             # Minimal API (redirect, create, analytics)
├── Shortener.Host.Functions/       # Azure Functions timer host for expiration/cleanup jobs
├── Shortener.ServiceDefaults/      # Shared Aspire, OpenTelemetry, resilience
├── Shortener.Domain/               # Domain models and logic
├── Shortener.Application/          # CQRS handlers (MediatR), vertical slices
├── Shortener.Application.Abstractions/  # Application contracts and interfaces
├── Shortener.Infrastructure.Abstractions/  # Persistence/cache abstractions
├── Shortener.Infrastructure.Shared/       # Shared infrastructure utilities
└── Shortener.Infrastructure.Database/     # Cosmos DB implementation
tests/
└── Shortener.Domain.Tests/         # Domain unit tests
docs/                               # Architecture, features, ADRs, tasks
```

---

## Documentation

| Topic                                                       | Document                                                                                    |
| ----------------------------------------------------------- | ------------------------------------------------------------------------                    |
| Solution overview, NFRs, testing                            | [docs/solution.md](docs/solution.md)                                                        |
| Architecture, components, deployment                        | [docs/architecture.md](docs/architecture.md)                                                |
| Features (shortening, redirect, analytics, security)        | [docs/features/index.md](docs/features/index.md)                                            |
| Architecture decision records                               | [docs/decisions.md](docs/decisions.md)                                                      |
| Task list and definition of done                            | [docs/tasks.md](docs/tasks.md) · [docs/definition-of-done.md](docs/definition-of-done.md)   |

Task 4 (`docs/tasks.md` - Expiration & Cleanup) is implemented using the `Shortener.Host.Functions` timer-trigger host for scheduled cleanup execution.

---

## Code Conventions

- **Formatting & style** — All code follows the root [.editorconfig](.editorconfig) (naming, indentation, C# rules).
- **Package versions** — Central Package Management ([Directory.Packages.props](Directory.Packages.props)); projects reference packages without a `Version` attribute.

---
