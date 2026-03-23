---
name: seq-dig
description: >
  Investigate Seq structured logs — search for errors, trace requests by correlation ID,
  detect error spikes, and find slow requests.
  Load this skill when: "seq", "seq logs", "correlation id", "log investigation",
  "/seq-dig", "trace request", "error spike", "structured logs", "seq query".
user-invocable: true
argument-hint: "[errors|trace|search|stats|slow] [correlation-id or search-term]"
allowed-tools: Read, Bash
---

# Seq Dig — Structured Log Investigation

## Core Principles

1. **Start broad, then narrow** — Begin with `/seq-dig errors` to get an overview of what's failing, then use `/seq-dig trace <correlationId>` to drill into specific request flows.
2. **Use correlation IDs to reconstruct request timelines** — Every HTTP request should have a `CorrelationId` property. Tracing it shows the full lifecycle: incoming request → handler → domain → persistence → response.
3. **Show message templates, not interpolated strings** — Log entries in Seq store both the template (`"Order {OrderId} created"`) and the rendered message. Display the template plus structured properties for maximum utility.
4. **Limit exception output** — Show the first 10 lines of a stack trace by default. Full stack traces consume screen space and the important part (exception type + first frame) is almost always at the top.
5. **Diagnose connectivity before failing** — If Seq is unreachable, check the Docker container status and the `SEQ_URL` config value before reporting an error to the user.

## Patterns

### Error Investigation Workflow

```
// GOOD — systematic investigation flow
1. /seq-dig errors 60           → what errors happened in the last hour?
2. /seq-dig stats 24            → is there an error spike or steady rate?
3. /seq-dig trace <corrId>      → trace one specific failing request
4. /seq-dig search "OrderId"    → find all logs mentioning a specific value

// BAD — jumping straight to trace without knowing the error type
/seq-dig trace abc-123          → you can't interpret the output without error context
```

### Seq API Query Format

```bash
# Errors in last 60 minutes
GET {SEQ_URL}/api/events?filter=@Level%3D'Error'%20OR%20@Level%3D'Fatal'&fromRelativeTimeSpan=60m&count=50

# Trace all events for a correlation ID
GET {SEQ_URL}/api/events?filter=CorrelationId%3D'{corrId}'&count=100&render=true

# Full-text search across message text
GET {SEQ_URL}/api/events?filter=@Message%20like%20'%25{term}%25'&count=20

# Slow HTTP requests (elapsed > threshold)
GET {SEQ_URL}/api/events?filter=Elapsed%3E{ms}&count=20
```

### Output Format for Log Entries

```
// GOOD — structured, readable output showing key properties
[14:32:01 ERR] Order.NotFound — Order was not found.
  RequestId:     abc-123-def-456
  CorrelationId: req-789
  OrderId:       550e8400-e29b-41d4-a716-446655440000
  UserId:        user-42
  Exception:     KeyNotFoundException: Order not found in database
                 at GetOrderHandler.Handle() line 34
                 at RequestHandlerDelegate`1.Invoke() ...

// BAD — raw JSON dump without formatting
{"@t":"2026-03-22T14:32:01Z","@l":"Error","@m":"Order was not found","OrderId":"550e8..."}
```

### Error Grouping for Stats

```
// GOOD — group errors by type to identify the biggest problems
Top Errors (last 60 minutes):
  1. KeyNotFoundException        — 47 occurrences  (Order.NotFound)
  2. ValidationException         — 12 occurrences  (invalid request data)
  3. SqlException timeout        —  3 occurrences  (database connectivity)

// Then show the most recent occurrence of each with full detail
// BAD — showing 50 individual error lines without grouping
[14:32:01] KeyNotFoundException: Order not found
[14:31:58] KeyNotFoundException: Order not found
[14:31:55] KeyNotFoundException: Order not found
... (47 more identical entries)
```

## Anti-patterns

### Showing Full Stack Traces by Default

```
// BAD — dumping complete stack traces for every error (unusable noise)
Exception: System.Collections.Generic.KeyNotFoundException: Order not found
  at MyApp.Application.Orders.Queries.GetOrderHandler.Handle(GetOrderQuery request, CancellationToken ct)
  at MediatR.Pipeline.RequestHandlerWrapperImpl`2.Handle(...)
  at MediatR.Mediator.Send(...)
  at Microsoft.AspNetCore.Routing.EndpointMiddleware.Invoke(...)
  [40 more frames...]

// GOOD — first 10 lines only, offer to show more
Exception: KeyNotFoundException: Order not found
  at GetOrderHandler.Handle() — Orders/Queries/GetOrderHandler.cs:34
  at MediatR pipeline (8 more frames hidden — ask to see full trace)
```

### Ignoring Connectivity Issues

```bash
# BAD — reporting "no errors found" when Seq is actually unreachable
curl http://localhost:5341/api/events → connection refused
# [return empty results as if everything is healthy]

# GOOD — check connectivity and diagnose
curl http://localhost:5341/api/health → connection refused
docker ps | grep seq  → seq container not running
→ "Seq is not reachable. The seq container appears to be stopped.
   Run: docker compose up -d seq"
```

### Searching Without Encoding

```bash
# BAD — URL with unencoded special characters fails silently
GET http://localhost:5341/api/events?filter=@Level='Error'

# GOOD — URL-encode the filter parameter
GET http://localhost:5341/api/events?filter=%40Level%3D'Error'
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| What errors happened recently? | `/seq-dig errors 60` |
| Trace one specific request | `/seq-dig trace <correlationId>` |
| Find logs mentioning an order/user/entity | `/seq-dig search <value>` |
| Is there an error spike? | `/seq-dig stats 24` |
| Which endpoints are slowest? | `/seq-dig slow 500` |
| Seq not responding | Check Docker: `docker ps \| grep seq`, check `SEQ_URL` in config |
| Error rate is high but errors look OK | Check `/seq-dig slow` — might be timeouts causing retries |
| Can't find a request by correlation ID | Verify correlation ID middleware is registered in `Program.cs` |

## Execution

### Prerequisites
Read `~/.claude/kit.config.md` for `SEQ_URL`. Default: `http://localhost:5341`.

The Seq API supports CLEF queries via: `GET {SEQ_URL}/api/events?filter=...&count=...`

If Seq is not reachable, diagnose:
1. Check if Seq Docker container is running: `docker ps | grep seq`
2. Check the URL in kit.config.md
3. Suggest: `docker compose up -d seq`

### `/seq-dig errors [minutes=60]`
Fetch recent errors:
```
GET {SEQ_URL}/api/events?filter=@Level='Error' OR @Level='Fatal'&fromRelativeTimeSpan={minutes}m&count=50
```
- Group by error type / exception message
- Show top 5 most frequent errors with occurrence count
- Display full message + exception (first 10 lines) for the most recent occurrence of each

### `/seq-dig trace <correlationId>`
Trace all events for a request:
```
GET {SEQ_URL}/api/events?filter=CorrelationId='{correlationId}'&count=100
```
- Show in chronological order
- Highlight warnings and errors
- Show timing between events (latency breakdown)

### `/seq-dig search <term>`
Full-text search:
```
GET {SEQ_URL}/api/events?filter=@Message like '%{term}%'&count=20
```

### `/seq-dig stats [hours=24]`
Error rate stats:
- Total events by level in the time window
- Error spike detection (errors per 5-min bucket)
- Top 5 slowest requests (from HTTP request logging)

### `/seq-dig slow [ms=1000]`
Find slow requests:
```
GET {SEQ_URL}/api/events?filter=Elapsed>{ms}&count=20
```
Sort by `Elapsed` descending. Show: path, method, status code, elapsed, correlation ID.

### Output Format
Always show:
- Timestamp (local time)
- Level
- Message template (not interpolated string)
- Key structured properties (CorrelationId, UserId, entity IDs)
- Exception if present — first 10 lines only, offer full trace on request

$ARGUMENTS
