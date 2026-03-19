# Observability

## Description

Logging, metrics, and tracing so the system can be monitored, debugged, and tuned. All services emit OpenTelemetry data where applicable (e.g., via .NET Aspire defaults).

## Requirements

- **Logging:** Structured logs (e.g., JSON); include correlation/trace IDs. Log all exceptions; no silent failures. Do not log sensitive data (e.g., full URLs only where necessary and safe).
- **Metrics:** Expose metrics for request counts, latency (e.g., redirect path, create), cache hit/miss ratio, error rates, and rate-limit hits. Use OpenTelemetry metrics or ASP.NET Core metrics.
- **Tracing:** Distributed tracing for requests (e.g., redirect, create, analytics). Use OpenTelemetry tracing; ensure trace context is propagated across async analytics path.

## Rules

- Use Aspire defaults for OpenTelemetry (logging, metrics, tracing) when running under Aspire.
- Health checks: liveness (process up), readiness (Cosmos DB and Redis reachable). See [architecture.md](../architecture.md) and [solution.md](../solution.md).
- Redirect and create paths should have clear span names and attributes (e.g., shortCode, cache hit/miss) for debugging performance.

## Related Features

- [Architecture](../architecture.md) — OpenTelemetry and health checks.
- [Solution overview](../solution.md) — observability section.
- All features — errors and performance are observable.

## Tests

- Integration: Verify logs contain expected structure and trace IDs.
- Optional: Smoke test health endpoints return 200 when dependencies are up.
