# URL Shortening

## Description

Creates a short URL for a given long URL. Each user gets a unique short URL: the **auto-generated** short code is from a counter (Base62), up to 7 characters. An optional **custom alias** is separate (longer, kebab-style; see below).

## Inputs

- **longUrl** (string, required): The URL to shorten. Must be a valid URL format.
- **alias** (string, optional): User-defined path segment. Must be unique if provided; up to 32 characters; letters, numbers, and **single hyphens between** alphanumeric segments (kebab-style, e.g. `my-link`). Leading, trailing, or double hyphens are invalid.

## Outputs

- **shortUrl** (string): Full short URL (e.g., `https://short.example.com/abc12XY`).
- **shortCode** (string): The auto-generated Base62 code (up to 7 characters) or the user-defined alias (up to 32 characters with kebab rules).

## Rules

- Generate short code using a **counter** converted to Base62 (a-z, A-Z, 0-9); maximum 7 characters.
- Counter allocation: use ranges from Cosmos DB to support horizontal scaling and avoid collisions.
- If **alias** is provided: it must be unique; reject with 409 (or 400) if already taken.
- **Idempotent creation (optional):** When enabled, same longUrl + alias returns existing short URL instead of creating a duplicate (see ADR-005).
- Validate URL format and length; reject invalid input with 400 and ProblemDetails.
- Persist mapping in Cosmos DB; optionally prime Redis cache after create.

## Edge Cases

- Invalid URL → 400 Bad Request (ProblemDetails).
- Duplicate alias → 409 Conflict (or 400 with clear message).
- Optional: duplicate longUrl + alias with idempotency enabled → 200/201 with existing short URL.
- Rate limit exceeded → 429 Too Many Requests.

## Dependencies

- Counter storage/allocation (Cosmos DB).
- Cosmos DB (persistence).
- Redis (optional cache prime).
- MediatR command (e.g., CreateShortUrlCommand).

## Related Features

- [Redirects](./redirects.md) — short code is used for redirect lookup.
- [Analytics](./analytics.md) — new short URL will accumulate clicks and last-accessed.
- [Expiration](./expiration.md) — optional expiration date can be set on create.
- [Security](./security.md) — input validation, rate limiting.

## Tests

- Unit: URL validation, Base62 generation from counter, alias uniqueness check, idempotent behavior when enabled.
- Integration: Create short URL via API; verify in Cosmos DB and optional Redis; duplicate alias returns error.
