# Redirects

## Description

Visiting a short URL (e.g., `GET /{shortCode}`) redirects the user to the original long URL. The redirect must be fast (target <100ms). Expired or deleted links return 404.

## Inputs

- **shortCode** (string): Path segment identifying the short link (e.g., from URL path). May be a **system-generated** Base62 code (up to 7 characters) or a **custom alias** (up to 32 characters, kebab-style segments). Custom aliases cannot use a reserved name that would collide with API, health, or app routes on the same host (see [URL Shortening](./url-shortening.md)).

## Outputs

- **Redirect:** HTTP 302 (or 301) with `Location` header set to the original long URL.
- **Not found:** HTTP 404 when short code is unknown, expired, or deleted (e.g., not accessed for one month).

## Rules

- **Performance target:** Redirect response in <100ms. Use Redis as read-through cache-aside: check Redis first; on miss, read from Cosmos DB and redirect **without** writing Redis on the redirect path (create may still prime Redis).
- Do not perform writes on the redirect path (analytics recorded asynchronously; see [Analytics](./analytics.md)).
- Expired links (optional expiration date passed) → 404.
- Links deleted due to inactivity (e.g., not accessed for 1 month) → 404.
- Invalid or empty shortCode → 404 or 400 as appropriate.
- After successful redirect, trigger async “record click” for analytics (click count, last-accessed timestamp).

## Edge Cases

- Short code not found → 404.
- Short code expired → 404.
- Short code deleted (inactivity) → 404.
- Cache miss: load from Cosmos DB and redirect (no cache write on miss; still target <100ms when Cosmos is fast enough).
- Rate limit exceeded → 429 (applies to all endpoints including redirect).

## Dependencies

- Redis (cache-aside lookup).
- Cosmos DB (authoritative data when cache misses).
- Optional expiration and TTL/inactivity logic (see [Expiration](./expiration.md)).
- Async analytics (see [Analytics](./analytics.md)).
- MediatR query (e.g., GetRedirectTargetQuery).

## Related Features

- [URL Shortening](./url-shortening.md) — produces the short codes used here.
- [Expiration](./expiration.md) — expiration and inactivity rules.
- [Analytics](./analytics.md) — click and last-accessed recording.
- [Security](./security.md) — rate limiting, throttling.

## Tests

- Unit: Handler logic for “found” vs “expired” vs “not found.”
- Integration: Redirect hit (from cache and from DB), 404 for missing/expired; latency under 100ms in tests.
- E2E: Create short URL, then GET short URL and assert redirect to long URL; GET expired code and assert 404.
