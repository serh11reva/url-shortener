# Architecture Decision Records (ADRs)

## ADR-001: URL Shortening Algorithm

**Context:** How do we generate a unique, maximum 7-character short code that is collision-resilient?

**Decision:** Use a numeric counter converted to a Base62 string (a-z, A-Z, 0-9). **Auto-generated** short codes are at most 7 characters. **Optional custom aliases** are a separate concept: users may supply their own path segment (different max length and charset—e.g., kebab-style with hyphens between alphanumeric segments) that must still be unique in the same namespace as generated codes.

**Rationale:** Hashing (e.g., MD5) requires collision checking and retries. A counter guarantees uniqueness for generated codes. To scale horizontally, the API can allocate ranges of counters from Cosmos DB (e.g., 1–1000) and consume them in memory. Custom aliases reuse the same lookup key shape in Cosmos (id/partition = short code or alias) without changing the counter algorithm.

---

## ADR-002: Redirection Performance

**Context:** Redirects must be fast (target <100ms).

**Decision:** Implement Cache-Aside with Redis. Redirect path: check Redis first; on miss, read from Cosmos DB and redirect **without** writing Redis on the redirect path (read-through). Creation may still prime Redis after persist.

**Rationale:** Hitting Cosmos DB for every redirect adds latency and RU cost. Redis provides low-latency key-value lookups and keeps the redirect path read-optimized.

---

## ADR-003: Handling Expiration & Deletion

**Context:** Links may have an optional expiration date; links not accessed for one month must be removed.

**Decision:** Use Cosmos DB Time-to-Live (TTL) where applicable. Set item TTL from the user’s optional expiration date. For the “delete if not accessed for one month” rule: when recording access (e.g., in analytics), refresh the item’s TTL or last-accessed field; a background process or TTL policy removes items that have not been accessed for 30 days.

**Rationale:** Cosmos DB supports item-level TTL, reducing custom cleanup logic. Analytics updates can maintain “last accessed” and drive deletion or TTL extension.

---

## ADR-004: Analytics Consistency

**Context:** Tracking clicks must not add write latency to the redirect path.

**Decision:** Accept eventual consistency for analytics. Use an in-process channel or queue to hand off “record click” work to a background handler. The redirect response is returned immediately; analytics (click count, last-accessed timestamp) are updated asynchronously.

**Rationale:** Redirect performance is the priority. Users do not need real-time click counts; eventual consistency is acceptable for analytics.

---

## ADR-005: Idempotent Creation (Optional)

**Context:** Should creating a short URL for the same long URL (and optional alias) multiple times return the same short URL?

**Decision:** Support optional idempotent creation. When enabled, if the same long URL (and alias, if provided) already exists, return the existing short URL and 200 (or 201 with same resource) instead of creating a duplicate.

**Rationale:** Reduces duplicate entries and improves UX when users resubmit the same link. Implemented as an optional behavior (e.g., query-by-longUrl+alias before create).

---

## ADR-006: Abuse Protection

**Context:** The API must be safe against abuse.

**Decision:** Apply basic rate limiting and throttling per IP (and optionally per key if auth is added later). Use standard ASP.NET Core rate-limiting middleware or equivalent. Return 429 when limits are exceeded.

**Rationale:** Protects the service from abuse and keeps behavior predictable. Per-IP is a simple, effective first step. **Horizontal scaling:** in-process limiters are **per replica**; see [ADR-014](#adr-014-per-instance-rate-limits-no-distributed-store).

---

## ADR-007: Error Response Format

**Context:** Errors must be consistent and machine-readable.

**Decision:** Use RFC 7807 ProblemDetails for all API error responses. Use appropriate HTTP status codes (400, 404, 429, 500). Do not expose internal details in production; log all exceptions.

**Rationale:** ProblemDetails is supported by Microsoft.AspNetCore.Mvc and gives clients a uniform structure. Aligns with “no silent failures” and consistent error handling.

---

## ADR-008: CQRS and MediatR Version

**Context:** Commands and queries should be clearly separated; a mediator keeps handlers decoupled.

**Decision:** Use CQRS with MediatR (in-process). **Pin MediatR to a version below 13.0** (e.g., 12.x) for compatibility and stability.

**Rationale:** MediatR fits vertical slices and keeps the pipeline (validation, logging) in one place. Version cap avoids breaking changes from major upgrades during the project lifecycle.

---

## ADR-009: Container and Deployment Layout

**Context:** Deployment must use Docker and support local and cloud runs.

**Decision:** Use Docker Compose for local/multi-service deployment. Each service (e.g., API, frontend if containerized) has its own Dockerfile. Production infrastructure is defined with modular Azure Bicep files.

**Rationale:** One Dockerfile per service keeps builds clear and reusable. Bicep modules allow reuse across environments and align with Azure as the target platform.

---

## ADR-010: Central Package Management

**Context:** How do we manage NuGet package versions across multiple projects?

**Decision:** Use **Central Package Management (CPM)** with a root `Directory.Packages.props` file. All package versions are defined there. Each project references packages with `<PackageReference Include="PackageId" />` only (no `Version` in the project). Enable CPM by setting `ManagePackageVersionsCentrally=true` (or using the SDK that supports it).

**Rationale:** Single place for versions; consistent versions across the solution; easier upgrades and fewer version conflicts. Aligns with modern .NET practices.

---

## ADR-011: EditorConfig for Code Rules

**Context:** How do we keep code style and formatting consistent for humans and AI agents?

**Decision:** Use a root **.editorconfig** file for code rules (formatting, naming, indentation, etc.). All contributors and AI agents must follow it when writing or modifying code.

**Rationale:** .editorconfig is supported by IDEs and many tools; it keeps the codebase consistent and gives agents clear, machine-readable rules to follow.

---

## ADR-012: Minimal API for Backend

**Context:** Should the backend use controller-based MVC or Minimal APIs?

**Decision:** Use **Minimal API** (ASP.NET Core minimal hosting). Endpoints are defined with `MapGet`, `MapPost`, etc. Route handlers dispatch to MediatR (commands/queries); business logic stays in handlers, not in endpoint lambdas. Do not add MVC controllers.

**Rationale:** Minimal API keeps the API surface explicit and lightweight; it fits vertical slices and CQRS (thin endpoints, MediatR for behavior). Good fit for a small, focused API and for portfolio clarity.

---

## ADR-013: Cleanup Execution Host

**Context:** Task 4 (Expiration & Cleanup) needs a scheduled execution mechanism to remove inactive links and apply expiration cleanup outside the redirect request path.

**Decision:** Use an **Azure Functions** project (`Shortener.Host.Functions`) with a **TimerTrigger** as the cleanup host. The function runs on a schedule and orchestrates cleanup logic (TTL reconciliation, inactive-link deletion, cache invalidation as needed). In local development, it is orchestrated by Aspire with Azurite host storage.

**Rationale:** Timer-triggered Functions are a managed, cloud-aligned way to run scheduled jobs without introducing a dedicated worker service runtime. This keeps the API path lean, supports independent scaling/deployment of cleanup behavior, and aligns with Azure-first deployment targets.

---

## ADR-014: Per-Instance Rate Limits (No Distributed Store)

**Context:** ASP.NET Core’s built-in rate limiting uses in-process partitions (e.g., `FixedWindowRateLimiter`). When the API runs multiple instances behind a load balancer, each instance maintains its own counters.

**Decision:** **Accept per-instance limits** as the operational model. We do **not** synchronize rate-limit state across replicas (no Redis-backed or other distributed limiter in the API for this concern). Document that the configured limit applies **per process**; with *N* healthy replicas, a client’s effective ceiling is roughly *N ×* the configured permits per window (assuming even load distribution), not a single global cap.

**Rationale:** Keeps the stack simple and avoids extra Redis traffic and failure modes on every request for throttling alone. Per-instance limiting still protects each replica from overload. If a strict global budget is required later (e.g., commercial API product), revisit with a distributed limiter or edge/gateway rate limiting (see also [ADR-006](#adr-006-abuse-protection)).
