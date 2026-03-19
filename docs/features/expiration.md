# Expiration

## Description

Short links can have an optional expiration date. Expired links return 404 when accessed. Links that have not been accessed for one month are deleted.

## Inputs (Create/Update)

- **expiresAt** (date/time, optional): When the link should expire. If omitted, link does not expire by date (but may still be removed by inactivity rule).

## Rules

- **Optional expiration date:** If set, any redirect attempt after that date returns 404. Redirect and lookup logic must check expiration before returning the long URL.
- **Delete if not accessed for one month:** Links that have no access for 30 days are removed. Use Cosmos DB TTL and/or last-accessed timestamp (from [Analytics](./analytics.md)) to implement; a background process or TTL policy can delete or expire items. When a link is deleted, redirect returns 404.
- Cache: When returning 404 due to expiration or deletion, avoid or invalidate caching of the “not found” result for the short code so that future lookups re-evaluate (or ensure TTL/cache invalidation is consistent).

## Edge Cases

- Redirect requested after expiresAt → 404.
- Redirect requested for link deleted due to inactivity → 404.
- Expiration date in the past on create → 400 (invalid).

## Dependencies

- Cosmos DB (store expiresAt; TTL or last-accessed for inactivity).
- [Analytics](./analytics.md) — last-accessed timestamp drives “not accessed for 1 month.”
- [Redirects](./redirects.md) — redirect handler enforces expiration and returns 404.

## Related Features

- [URL Shortening](./url-shortening.md) — optional expiresAt on create.
- [Redirects](./redirects.md) — 404 for expired/deleted.
- [Analytics](./analytics.md) — last-accessed for inactivity rule.

## Tests

- Unit: Expiration check logic (expired vs not expired).
- Integration: Create link with expiresAt in past → 400; create with future expiresAt, wait or mock time, redirect → 404.
- Integration: Verify links not accessed for 30 days are removed (or TTL applied) and redirect returns 404.
