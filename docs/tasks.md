# Task List for AI Agents

This document provides a prioritized list of work items for building the URL shortener. Agents should use it in conjunction with [solution.md](./solution.md), [architecture.md](./architecture.md), [features/](./features/index.md), and [definition-of-done.md](./definition-of-done.md). Complete items in an order that respects dependencies (e.g., API before frontend, core features before analytics).

---

## 1. Foundation & Infrastructure

- [x] **1.0** **Repo conventions:** Ensure root [.editorconfig](../.editorconfig) exists and is followed by all generated code. Create root [Directory.Packages.props](../Directory.Packages.props) and enable Central Package Management; all .NET projects must reference packages without a `Version` attribute (see [decisions.md](./decisions.md) ADR-010, ADR-011).
- [x] **1.1** Create solution structure: .NET 10 **Minimal API** (not MVC controllers), vertical slices (e.g., CreateShortUrl, Redirect, Analytics), CQRS with MediatR (<13.0). Use CPM for all package versions (see ADR-012).
- [x] **1.2** Add Cosmos DB and Redis client libraries and configuration (connection strings, container/keys). Add package references via CPM only.
- [x] **1.3** Configure .NET Aspire host and add API, Redis, and Cosmos DB (or emulators) for local development.
- [x] **1.4** Add health checks (liveness, readiness) including Cosmos DB and Redis.
- [x] **1.5** Add OpenTelemetry (logging, metrics, tracing) via Aspire defaults.
- [x] **1.6** Implement global error handling: RFC 7807 ProblemDetails, proper HTTP status codes, log all exceptions (no silent failures).
- [x] **1.7** Add rate limiting and throttling per IP (middleware); return 429 when exceeded.

---

## 2. URL Shortening (Create)

- [x] **2.1** Implement counter-based short code generation: Base62, max 7 characters; allocate counter ranges from Cosmos DB for scalability.
- [x] **2.2** Implement CreateShortUrl command/handler: validate long URL, optional user-defined alias; enforce uniqueness for alias; persist to Cosmos DB; optionally cache in Redis.
- [x] **2.3** Support optional idempotent creation: same long URL + alias returns existing short URL (see ADR-005).
- [x] **2.4** Expose REST endpoint (e.g., POST /api/urls) with validation; return short URL in response.
- [x] **2.5** Unit tests: validation, Base62 generation, collision-free behavior. Integration tests: create flow with Cosmos DB (or emulator).

---

## 3. Redirect

- [x] **3.1** Implement GetRedirectTarget query/handler: lookup by short code; check Redis first (cache-aside), then Cosmos DB; consider expiration and “not accessed for 1 month” (return 404 when expired or deleted).
- [x] **3.2** Implement redirect endpoint (e.g., GET /{shortCode}): 302/301 to long URL; 404 for missing or expired. Target latency <100ms.
- [x] **3.3** On cache miss: load from Cosmos DB, validate (not expired, not deleted), cache in Redis, then redirect.
- [x] **3.4** Integration tests: redirect hit, cache miss, expired link 404. E2E: create then redirect.

---

## 4. Expiration & Cleanup

- [ ] **4.1** Support optional expiration date on create (store in Cosmos DB); on redirect, return 404 if expired.
- [ ] **4.2** Implement “delete if not accessed for one month”: tie to analytics (last-accessed); use Cosmos DB TTL or background job to remove inactive links (see ADR-003).
- [ ] **4.3** Invalidate or omit Redis cache for expired/deleted links when detected.
- [ ] **4.4** Tests: expiration date respected (404); TTL or job removes old items.

---

## 5. Analytics

- [ ] **5.1** On redirect: enqueue or channel “record click” (short code, timestamp); process asynchronously (no write on redirect path).
- [ ] **5.2** Analytics handler: update Cosmos DB with click count and last-accessed timestamp (eventual consistency).
- [ ] **5.3** Expose analytics read (e.g., GET /api/urls/{shortCode}/stats): number of clicks, last accessed timestamp.
- [ ] **5.4** Use last-accessed to drive “not accessed for 1 month” deletion/TTL (align with 4.2).
- [ ] **5.5** Tests: unit for handler; integration for eventual consistency; E2E for click count visibility.

---

## 6. Frontend (Vue 3)

- [ ] **6.1** Vue 3 app (Vite, TypeScript); Composition API with `<script setup>`; small, focused components.
- [ ] **6.2** Page: submit long URL and optional alias; display short URL and link to stats.
- [ ] **6.3** Page or section: view analytics (clicks, last accessed) for a short code.
- [ ] **6.4** E2E tests: create short URL, open short URL (redirect), view analytics.

---

## 7. Containers & Deployment

- [ ] **7.1** Dockerfile for API (multi-stage build).
- [ ] **7.2** Dockerfile for frontend (build static assets; serve via nginx or API).
- [ ] **7.3** Docker Compose: API, Redis, Cosmos DB (or emulator), frontend (if separate).
- [ ] **7.4** Modular Bicep: resource group, Cosmos DB, Redis, App Service or Container Apps; parameterized for environment.

---

## 8. CI/CD

- [ ] **8.1** Pipeline: build solution; run unit tests.
- [ ] **8.2** Pipeline: run integration tests (with emulators or test containers if applicable).
- [ ] **8.3** Pipeline: build Docker images for API (and frontend if applicable).
- [ ] **8.4** Pipeline: deploy infrastructure (Bicep) to target environment.
- [ ] **8.5** Pipeline: deploy application (containers or artifacts) to Azure.

---

## 9. Documentation & DoD

- [ ] **9.1** Ensure README has: how to run locally (Aspire), how to run with Docker Compose, env vars, and link to docs.
- [ ] **9.2** All features and tasks verified against [definition-of-done.md](./definition-of-done.md).

---

Agents: when picking a task, read the relevant feature doc under [features/](./features/index.md) and the linked ADRs in [decisions.md](./decisions.md). Mark tasks complete only when the Definition of Done is satisfied.
