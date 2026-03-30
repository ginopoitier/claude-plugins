# OpenTelemetry — .NET 9 Reference

## Overview

OpenTelemetry .NET provides a vendor-neutral SDK for traces, metrics, and logs. The OTLP exporter sends all three signals to any compatible backend (Grafana, Jaeger, Honeycomb, Datadog, Azure Monitor). In .NET 9, `AddOpenTelemetry()` is the single entry point; ASP.NET Core, EF Core, HttpClient, and the runtime emit built-in instrumentation automatically with no extra code. Custom spans and metrics layer on top via `ActivitySource` and `Meter`.

## Setup: Complete Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config,
    IHostEnvironment env)
{
    // Service name and version appear on every span/metric/log record
    var serviceName    = config["Telemetry:ServiceName"] ?? "MyApp";
    var serviceVersion = config["Telemetry:ServiceVersion"] ?? "1.0.0";

    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(
                serviceName:    serviceName,
                serviceVersion: serviceVersion)
            // Adds host.name, os.type, process.* etc. — useful for filtering in dashboards
            .AddDetector<EnvironmentResourceDetector>())

        // ── TRACES ────────────────────────────────────────────────────────────────
        .WithTracing(tracing => tracing
            // ASP.NET Core: traces every inbound HTTP request automatically
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;  // attaches exception details to failed spans
                // Exclude health check endpoints to reduce noise
                options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            })
            // HttpClient: traces every outbound HTTP call with URL, status code, duration
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                // Redact auth headers so tokens don't appear in trace backends
                options.FilterHttpRequestMessage = msg =>
                    msg.RequestUri?.Host != "169.254.169.254";  // exclude IMDSv2 calls
            })
            // EF Core: traces queries; set SetDbStatementForText=true only in dev (PII risk)
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = env.IsDevelopment();
            })
            // Custom ActivitySources registered by application code
            .AddSource(Telemetry.ActivitySourceName)
            // OTLP export — send to collector; use env vars for the endpoint in production
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(config["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                otlp.Protocol = OtlpExportProtocol.Grpc;
            }))

        // ── METRICS ───────────────────────────────────────────────────────────────
        .WithMetrics(metrics => metrics
            // ASP.NET Core: request rate, duration, active requests per endpoint
            .AddAspNetCoreInstrumentation()
            // HttpClient: outbound request rate and duration per host
            .AddHttpClientInstrumentation()
            // .NET runtime: GC, threadpool, memory — critical for diagnosing latency spikes
            .AddRuntimeInstrumentation()
            // Custom Meters registered by application code
            .AddMeter(Telemetry.MeterName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(config["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                otlp.Protocol = OtlpExportProtocol.Grpc;
            }))

        // ── LOGS ──────────────────────────────────────────────────────────────────
        .WithLogging(logging => logging
            // Structured logs include trace_id and span_id automatically when a trace is active
            // This enables log-to-trace correlation in Grafana / Tempo
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(config["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                otlp.Protocol = OtlpExportProtocol.Grpc;
            }));

    return services;
}
```

## Pattern: Custom ActivitySource (Spans)

Use `ActivitySource` to create child spans inside your own code. Spans created this way automatically nest under the incoming HTTP request span.

```csharp
// Infrastructure/Telemetry/Telemetry.cs
// Centralize source/meter names to avoid typos across the codebase
public static class Telemetry
{
    public const string ActivitySourceName = "MyApp";
    public const string MeterName          = "MyApp";

    // ActivitySource is thread-safe and cheap to keep as a static singleton
    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName, "1.0.0");

    // Meter is also a singleton; Instruments (Counter, Histogram) are created from it
    public static readonly Meter Meter =
        new(MeterName, "1.0.0");

    // Pre-create instruments once — creating them per-request is expensive
    public static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>(
            "orders.created",
            unit: "{order}",
            description: "Total number of orders successfully created.");

    public static readonly Histogram<double> PaymentDuration =
        Meter.CreateHistogram<double>(
            "payment.duration",
            unit: "ms",
            description: "Duration of payment gateway calls in milliseconds.");
}

// Application/Orders/Commands/CreateOrderHandler.cs
internal sealed class CreateOrderHandler(AppDbContext db, IPaymentGatewayClient payment)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // Start a custom span that nests under the MediatR pipeline span
        using var activity = Telemetry.ActivitySource.StartActivity("CreateOrder");

        // Tags appear as filterable attributes in your trace backend
        activity?.SetTag("order.customer_id", cmd.CustomerId);
        activity?.SetTag("order.amount",       cmd.Amount);

        var order = Order.Create(new CustomerId(cmd.CustomerId), new Money(cmd.Amount, "GBP"));
        db.Orders.Add(order);

        // Nested span for the payment step — makes it easy to see where time is spent
        using (var paymentActivity = Telemetry.ActivitySource.StartActivity("ChargePayment"))
        {
            paymentActivity?.SetTag("payment.provider", "stripe");
            var sw = Stopwatch.StartNew();

            var paymentResult = await payment.ChargeAsync(
                new ChargeRequest(order.Id.Value, cmd.Amount), ct);

            // Record a metric: histogram buckets let you calculate p50/p95/p99 in dashboards
            Telemetry.PaymentDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new TagList { { "payment.success", paymentResult.IsSuccess } });

            if (paymentResult.IsFailure)
            {
                // Mark the span as failed so it shows red in Jaeger/Tempo
                paymentActivity?.SetStatus(ActivityStatusCode.Error, paymentResult.Error!.Description);
                return paymentResult.Error!;
            }
        }

        await db.SaveChangesAsync(ct);

        // Increment the counter metric — tagged so dashboards can filter by currency
        Telemetry.OrdersCreated.Add(1,
            new TagList { { "order.currency", "GBP" } });

        // Attach the order ID to the parent span for cross-service correlation
        activity?.SetTag("order.id", order.Id.Value);

        return order.Id.Value;
    }
}
```

## Pattern: MediatR Pipeline Behavior for Automatic Tracing

Add a pipeline behavior so every MediatR request gets a span without modifying each handler.

```csharp
// Application/Behaviors/TracingBehavior.cs
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        // Each MediatR request type gets its own span name — visible in the trace waterfall
        using var activity = Telemetry.ActivitySource.StartActivity(
            $"MediatR: {requestName}",
            ActivityKind.Internal);

        activity?.SetTag("mediator.request", requestName);

        try
        {
            var response = await next();

            // If the response is a Result<T>, surface success/failure on the span
            if (response is IResult result && result.IsFailure)
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.Error?.Description);
                activity?.SetTag("mediator.success", false);
            }
            else
            {
                activity?.SetTag("mediator.success", true);
            }

            return response;
        }
        catch (Exception ex)
        {
            // Record exception details on the span before rethrowing
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}

// Register in Application/DependencyInjection.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
});
```

## Pattern: Structured Logging with Trace Correlation

When using OTLP log export, `trace_id` and `span_id` are added automatically. If using Serilog alongside OTel, configure the enricher to ensure the same IDs appear in log sinks.

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       // Enriches every log entry with the active OpenTelemetry trace/span IDs
       // so Grafana can pivot from a log line to the full trace
       .Enrich.WithOpenTelemetryTraceId()
       .Enrich.WithOpenTelemetrySpanId()
       .WriteTo.Console(new RenderedCompactJsonFormatter())
       .WriteTo.Seq(ctx.Configuration["Seq:Url"]!);
});
```

## Anti-patterns

### Don't create ActivitySource or Meter per request

```csharp
// BAD — ActivitySource construction is expensive; creating one per handler invocation
//       causes GC pressure and leaks native handles if not disposed
public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    using var source   = new ActivitySource("MyApp");   // new object every call
    using var activity = source.StartActivity("CreateOrder");
    // ...
}

// GOOD — use the static singleton declared once in the Telemetry class
using var activity = Telemetry.ActivitySource.StartActivity("CreateOrder");
```

### Don't put sensitive data in span tags

```csharp
// BAD — PII in tags is exported to your observability backend and stored in plain text
activity?.SetTag("customer.email",       customer.Email);
activity?.SetTag("payment.card_number",  cardNumber);

// GOOD — use opaque IDs; keep business context without leaking PII
activity?.SetTag("customer.id", customer.Id.Value);   // non-reversible identifier
activity?.SetTag("payment.provider", "stripe");
activity?.SetTag("payment.currency", "GBP");
```

### Don't suppress the ambient activity in background workers

```csharp
// BAD — ActivityContext.Current is null in background threads when not propagated;
//       spans created here are orphaned and don't appear in the correct trace
Task.Run(() =>
{
    using var activity = Telemetry.ActivitySource.StartActivity("BackgroundWork");
    // This span has no parent — it floats disconnected in Jaeger
});

// GOOD — capture the current context and restore it in the background thread
var parentContext = Activity.Current?.Context ?? default;
Task.Run(() =>
{
    using var activity = Telemetry.ActivitySource.StartActivity(
        "BackgroundWork",
        ActivityKind.Internal,
        parentContext);    // re-parent to the originating request span
    // ...
});
```

## Reference

**NuGet Packages:**
```
OpenTelemetry                                          1.10.*
OpenTelemetry.Extensions.Hosting                      1.10.*
OpenTelemetry.Instrumentation.AspNetCore               1.10.*
OpenTelemetry.Instrumentation.Http                     1.10.*
OpenTelemetry.Instrumentation.EntityFrameworkCore      1.10.*
OpenTelemetry.Instrumentation.Runtime                  1.10.*
OpenTelemetry.Exporter.OpenTelemetryProtocol           1.10.*
Serilog.Enrichers.OpenTelemetry                        1.*
```

**Configuration (appsettings.json):**
```json
{
  "Telemetry": {
    "ServiceName":    "MyApp",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint":   "http://localhost:4317"
  }
}
```

**Docker Compose: local OTEL Collector + Jaeger:**
```yaml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686" # Jaeger UI
      - "14250:14250" # gRPC from collector
```
