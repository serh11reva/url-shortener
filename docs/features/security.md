# Security

## Description

Input validation, abuse protection, rate limiting per IP, and basic throttling so the system is safe against abuse and behaves predictably under load.

## Requirements

- **Input validation:** Validate all inputs (URLs, short codes, aliases). Reject invalid format, length, or dangerous content with 400 and RFC 7807 ProblemDetails. Do not allow raw user input to drive injection (e.g., sanitize or parameterize).
- **Abuse protection:** Limit how often a client can call the API to prevent abuse and resource exhaustion.
- **Rate limiting per IP:** Apply rate limits per client IP (e.g., N requests per minute per IP). Return 429 Too Many Requests with Retry-After when exceeded. Use ASP.NET Core rate-limiting middleware or equivalent. **Default:** 100 requests per minute per IP (fixed window); partition key is `RemoteIpAddress` or `X-Forwarded-For` when behind a proxy.
- **Basic throttling:** Throttle expensive or write-heavy operations (e.g., create short URL) more aggressively than read-only redirects if needed; document limits.

## Rules

- All API errors use ProblemDetails; do not expose stack traces or internal details in production.
- Log rate-limit hits and validation failures for monitoring; do not log sensitive data (e.g., full URLs only where necessary and safe).
- Redirect endpoint is read-heavy; rate limit it to prevent abuse (e.g., per-IP limit still applies).
- Create endpoint: validate URL and alias; apply rate limit and optional stricter throttling.

## Edge Cases

- Invalid URL or alias format → 400.
- Rate limit exceeded → 429, Retry-After when applicable.
- Very long URL or alias → 400 (length limits; custom alias max 32 characters).
- Malformed or missing required fields → 400.

## Dependencies

- ASP.NET Core rate-limiting (or equivalent).
- Validation (FluentValidation, DataAnnotations, or manual) in pipeline or handlers.
- [URL Shortening](./url-shortening.md), [Redirects](./redirects.md) — endpoints that receive validated input and rate limiting.

## Related Features

- [URL Shortening](./url-shortening.md) — validation and throttling on create.
- [Redirects](./redirects.md) — rate limiting on redirect.
- [Error handling](../solution.md#error-handling) — consistent ProblemDetails and status codes.

## Tests

- Unit: Validation rules for URL and alias.
- Integration: Exceed rate limit → 429; valid request after cooldown → 200.
- Integration: Invalid URL or alias → 400 with ProblemDetails.
