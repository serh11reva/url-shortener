# Analytics

## Description

Track usage of short links: number of clicks and last-accessed timestamp. Data is updated asynchronously (eventual consistency) so the redirect path stays fast (<100ms).

## Data Captured

- **Click count:** Total number of redirects (clicks) for the short code.
- **Last accessed timestamp:** When the short link was last used (e.g., last redirect time).

## Rules

- **No writes on redirect path:** Recording a click must not block the redirect. The API publishes a small JSON message to **Azure Service Bus** (queue); an **Azure Function** consumes messages and updates Cosmos DB asynchronously.
- **Idempotent analytics:** Each redirect generates a unique **click id** (included in the message body and as Service Bus **MessageId**). The queue should have **duplicate detection** enabled so accidental double-publishes with the same `MessageId` are dropped within the detection window. The worker applies a **durable idempotency** marker in Cosmos DB (transactional batch with the counter patch) so **redelivery** of the same message does not increment twice.
- **Eventual consistency:** Accept that click count and last-accessed may lag by seconds (or more under load). API that returns analytics (e.g., GET /api/urls/{shortCode}/stats) reads from Cosmos DB.
- **Last-accessed and inactivity:** Last-accessed timestamp is used to implement “delete if not accessed for one month” (see [Expiration](./expiration.md)); ensure analytics handler updates this field (and optionally TTL) on each click.
- Store data in Cosmos DB (same container or dedicated; design for read and batch/eventual write patterns).

## API (Read)

- **GET /api/urls/{shortCode}/stats** (or equivalent): Returns click count and last-accessed timestamp. Return 404 if short code does not exist or is expired/deleted.

## Edge Cases

- Short code not found or expired → 404 for stats endpoint.
- High redirect volume: analytics handler must not block redirects; use batching or backpressure if needed.
- New short link: click count 0, last-accessed null or not set until first click.

## Dependencies

- Cosmos DB (persist click count and last-accessed).
- Azure Service Bus queue; Azure Functions Service Bus trigger; MediatR `RecordClick` command handler.
- **Azure (production):** Create the `clicks` queue with **duplicate detection enabled** (cannot be toggled on later). Set `DuplicateDetectionHistoryTimeWindow` to at least the publisher retry window (e.g. 10 minutes). **Docker Compose** local runs use [docker/sb-emulator-config.json](../../docker/sb-emulator-config.json) with duplicate detection on; the Aspire Service Bus emulator may differ—Cosmos idempotency still prevents double counts if the broker redelivers.
- [Redirects](./redirects.md) — triggers “record click” on each redirect.
- [Expiration](./expiration.md) — last-accessed drives inactivity deletion.

## Related Features

- [Redirects](./redirects.md) — triggers analytics on each redirect.
- [Expiration](./expiration.md) — last-accessed for 1-month inactivity rule.
- [URL Shortening](./url-shortening.md) — short codes that receive analytics.

## Tests

- Unit: Analytics handler updates count and last-accessed correctly.
- Integration: Redirect several times; eventually stats show correct count and last-accessed (eventual consistency).
- E2E: Create short URL, redirect multiple times, call stats endpoint and assert counts and last-accessed.
