# Definition of Done

Every feature, task, or work item must satisfy the following before it is considered complete. AI agents should use this checklist when implementing and when marking tasks done in [tasks.md](./tasks.md).

---

## Code & Design

- [ ] **Architecture:** Implementation follows [architecture.md](./architecture.md): vertical slices, modular monolith, CQRS with MediatR (version <13.0), and the component boundaries (API, Redis, Cosmos DB, async analytics).
- [ ] **Decisions:** ADRs in [decisions.md](./decisions.md) are respected (e.g., counter-based short codes, cache-aside, ProblemDetails, rate limiting).
- [ ] **EditorConfig:** All new or modified code conforms to the repository [.editorconfig](../.editorconfig) (formatting, naming, style).
- [ ] **Central Package Management:** Package references use CPM: no `Version` in `.csproj`; versions are defined only in [Directory.Packages.props](../Directory.Packages.props) (see ADR-010).
- [ ] **No silent failures:** All exceptions are logged; errors are returned to the client or rethrown as appropriate—no swallowed errors.
- [ ] **Errors:** API errors use RFC 7807 ProblemDetails and correct HTTP status codes (400, 404, 429, 500).

---

## Functionality

- [ ] **Acceptance criteria:** All acceptance criteria for the feature (from [features/](./features/index.md) and linked feature docs) are met.
- [ ] **Edge cases:** Invalid input returns 400; not found/expired returns 404; rate limit returns 429.
- [ ] **Performance:** Redirect path meets the <100ms target where Redis is primed (cache-aside reads); Cosmos fallback on miss is acceptable when not cached; analytics do not block redirects.

---

## Testing

- [ ] **Unit tests:** New logic (e.g., validation, short-code generation, handlers) has unit tests.
- [ ] **Integration tests:** API flows that touch Cosmos DB or Redis have integration tests (using emulators or test containers where applicable).
- [ ] **E2E (where applicable):** Critical user journeys (create short URL, redirect, expired 404, analytics) have E2E coverage when in scope for the task.

### UI-only changes (`src/Shortener.Client.Web/`)

When the work is **limited to the Vue app** (no API, shared library, infrastructure, or contract changes):

- [ ] **Vue validation:** From `src/Shortener.Client.Web/`, run `npm ci` (or `npm install`), then `npm run lint` and `npm run build`.
- [ ] **Tests:** Add or update frontend tests only if the task introduces or changes testable UI behavior and tests are in scope; **do not** require `dotnet test` for a pure UI-only task.
- Skip the full .NET checklist items that do not apply (e.g., ProblemDetails, Cosmos integration tests) unless the task also touches the backend.

---

## Security & Resilience

- [ ] **Input validation:** All inputs (URLs, short codes, aliases) are validated; no raw injection into storage or responses.
- [ ] **Abuse protection:** Rate limiting and throttling (per IP) are in place and tested where relevant.

---

## Observability & Operations

- [ ] **Logging:** Meaningful structured logs; no sensitive data in logs.
- [ ] **Metrics/Tracing:** New endpoints or critical paths are covered by OpenTelemetry (Aspire defaults or explicit instrumentation).
- [ ] **Health:** New dependencies are reflected in readiness checks if they affect the service’s ability to serve traffic.

---

## Deployment & Documentation

- [ ] **Containers:** If the change adds or changes a service, it has a Dockerfile and is included in Docker Compose.
- [ ] **IaC:** If the change adds Azure resources, Bicep is updated in a modular way.
- [ ] **README / docs:** Any new env vars, run steps, or configuration are documented (e.g., in README or docs).

---

## Checklist Summary

Before marking a task complete:

1. Code matches architecture and ADRs.
2. No silent failures; errors use ProblemDetails (for API/backend changes).
3. Acceptance criteria and edge cases are covered.
4. Unit (and where relevant integration/E2E) tests are added and passing—or, for **UI-only** tasks, Vue `lint` and `build` pass per the UI-only subsection above.
5. Security (validation, rate limiting) is in place where applicable.
6. Observability and health are considered.
7. Deployment (Docker, Bicep) and docs are updated if the change affects them.
