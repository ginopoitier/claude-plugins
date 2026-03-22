---
name: opentelemetry
description: >
  OpenTelemetry observability for .NET applications. Covers traces, metrics,
  and logs using the OpenTelemetry SDK with OTLP export. Includes custom
  ActivitySource, IMeterFactory metrics, resource configuration, and Aspire
  Dashboard integration.
  Load this skill when setting up distributed tracing, custom metrics, OTLP
  export, or when the user mentions "OpenTelemetry", "OTLP", "traces", "spans",
  "Activity", "ActivitySource", "metrics", "IMeterFactory", "Meter", "Counter",
  "Histogram", "Gauge", "telemetry", "observability", "distributed tracing",
  "OTEL", or "Aspire Dashboard".
user-invocable: true
argument-hint: "[feature or service to instrument]"
allowed-tools: Read, Write, Edit, Bash
---

# OpenTelemetry

## Core Principles

1. **Three pillars, one setup** — Configure traces, metrics, and logs through a single `AddOpenTelemetry()` call. Use `UseOtlpExporter()` for cross-cutting export to any OTLP-compatible backend.
2. **Use `IMeterFactory` for metrics** — Never create `Meter` instances with `new`. The factory manages lifetime through DI and prevents leaks.
3. **Null-safe activities** — `StartActivity()` returns `null` when no listener is attached. Always use `?.` when setting tags or events.
4. **Environment variables over code** — Use `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` so deployments control telemetry routing without code changes.
5. **Low-cardinality metric tags** — Keep metric tag combinations under ~1000 per instrument. Use span attributes or logs for high-cardinality data like user IDs or request IDs.

## Patterns

### Full Setup with All Three Signals

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MyApp.Orders"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApp.Orders"))
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Cross-cutting OTLP export for traces + metrics (configured via env vars)
builder.Services.AddOpenTelemetry()
    .UseOtlpExporter();
```

The OTLP endpoint defaults to `http://localhost:4317` (gRPC). Override via:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
OTEL_SERVICE_NAME=MyApp.Api
```

### Custom Metrics with IMeterFactory

Register a metrics class as a singleton. `IMeterFactory` handles `Meter` disposal through DI.

```csharp
public sealed class OrderMetrics
{
    private readonly Counter<int> _ordersCreated;
    private readonly Histogram<double> _orderDuration;
    private readonly UpDownCounter<int> _activeOrders;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp.Orders");

        _ordersCreated = meter.CreateCounter<int>(
            "myapp.orders.created", "{orders}", "Number of orders created");

        _orderDuration = meter.CreateHistogram<double>(
            "myapp.orders.duration", "s", "Order processing duration",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.5, 1, 5, 10]
            });

        _activeOrders = meter.CreateUpDownCounter<int>(
            "myapp.orders.active", "{orders}", "Currently active orders");
    }

    public void OrderCreated() => _ordersCreated.Add(1);
    public void RecordDuration(double seconds) => _orderDuration.Record(seconds);
    public void OrderStarted() => _activeOrders.Add(1);
    public void OrderCompleted() => _activeOrders.Add(-1);
}

// Registration
builder.Services.AddSingleton<OrderMetrics>();
```

### Custom ActivitySource for Distributed Tracing

```csharp
public sealed class OrderService(ILogger<OrderService> logger)
{
    private static readonly ActivitySource Source = new("MyApp.Orders");

    public async Task<Order> ProcessOrderAsync(CreateOrderRequest request, CancellationToken ct)
    {
        using var activity = Source.StartActivity("ProcessOrder", ActivityKind.Internal);
        activity?.SetTag("order.customer_id", request.CustomerId);

        try
        {
            await ValidateOrder(request, ct);
            activity?.AddEvent(new ActivityEvent("OrderValidated"));

            var order = await SaveOrder(request, ct);
            activity?.SetTag("order.id", order.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

Register the source: `.AddSource("MyApp.Orders")` in the tracing builder.

### Aspire Dashboard for Local Development

Run the standalone Aspire Dashboard without Aspire orchestration:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 \
    mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then point your app at it:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

Dashboard UI is at `http://localhost:18888`.

### Source-Generated Logging with OTel

For maximum performance, use `[LoggerMessage]` — eliminates boxing and allocations.

```csharp
public partial class OrderService(ILogger<OrderService> logger)
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processing order {OrderId} for customer {CustomerId}")]
    partial void LogOrderProcessing(Guid orderId, Guid customerId);
}
```

OpenTelemetry logging automatically includes `TraceId` and `SpanId` when an `Activity` is current.

## Anti-patterns

### Don't Create Meters Per Request

```csharp
// BAD — new Meter per request causes memory leaks
public void HandleRequest()
{
    var meter = new Meter("MyApp");
    meter.CreateCounter<int>("requests").Add(1);
}

// GOOD — singleton via IMeterFactory
public class MyMetrics(IMeterFactory meterFactory)
{
    private readonly Counter<int> _requests =
        meterFactory.Create("MyApp").CreateCounter<int>("myapp.requests");
    public void RequestHandled() => _requests.Add(1);
}
```

### Don't Skip Null Checks on Activity

```csharp
// BAD — NullReferenceException when no listener is attached
using var activity = source.StartActivity("Work");
activity.SetTag("key", "value");

// GOOD — null-safe
activity?.SetTag("key", "value");
```

### Don't Mix UseOtlpExporter with AddOtlpExporter

```csharp
// BAD — throws NotSupportedException at runtime
builder.Services.AddOpenTelemetry()
    .UseOtlpExporter()
    .WithTracing(t => t.AddOtlpExporter());

// GOOD — use one approach
builder.Services.AddOpenTelemetry().UseOtlpExporter();
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Full observability setup | `AddOpenTelemetry()` with all three signals + `UseOtlpExporter()` |
| Custom business metrics | `IMeterFactory` + singleton metrics class |
| Custom trace spans | `ActivitySource` + `StartActivity()` |
| Local development backend | Aspire Dashboard standalone container |
| Production backend | OTel Collector as intermediary to Grafana/Datadog/etc. |
| High-performance logging | `[LoggerMessage]` source generator |
| Metric tag cardinality | Max ~1000 combinations per instrument |
| Environment configuration | `OTEL_*` env vars |
