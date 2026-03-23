---
name: scalar
description: >
  Scalar API documentation UI for .NET applications. Covers setup, themes,
  authentication prefill, multiple documents, layout options, and security.
  A modern replacement for Swagger UI.
  Load this skill when setting up API documentation UI, or when the user mentions
  "Scalar", "MapScalarApiReference", "API reference", "Swagger UI replacement",
  "API documentation UI", "Scalar theme", "interactive API docs", or "Try It".
user-invocable: true
argument-hint: "[theme, auth scheme, or document name]"
allowed-tools: Read, Write, Edit, Bash
---

# Scalar

## Core Principles

1. **Scalar replaces Swagger UI** — Scalar is the recommended API documentation UI for .NET 10. Faster rendering, built-in dark mode, code generation for dozens of languages, and full OpenAPI 3.1 support.
2. **Development only by default** — Wrap `MapScalarApiReference()` in an `IsDevelopment()` check. API documentation exposes internal structure.
3. **Disable the proxy for sensitive APIs** — Scalar's "Try It" feature routes through `proxy.scalar.com` by default. Disable it with `.WithProxy(null)` to keep auth headers local.
4. **Security schemes come from OpenAPI** — Scalar reads security schemes from the OpenAPI document. Configure them via document transformers, not in Scalar directly.

## Patterns

### Basic Setup

```csharp
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();  // UI at /scalar/v1
}
```

### Customized Configuration

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Checkout API")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithPreferredScheme("Bearer")
        .WithProxy(null)  // Disable external proxy
        .WithSidebar(true);
});
```

### Authentication Prefill (Development Only)

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithPreferredScheme("Bearer")
            .AddHttpAuthentication("Bearer", auth =>
            {
                auth.Token = "dev-only-test-token";
            });
    });
}
```

### Available Themes

```csharp
// ScalarTheme options: Default, Moon, Purple, BluePlanet, Saturn, Mars, DeepSpace, Kepler, Solarized, Laserwave
options.WithTheme(ScalarTheme.Mars);
```

### Multiple API Documents

```csharp
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2-beta");

app.MapOpenApi();
app.MapScalarApiReference();
// Available at /scalar/v1 and /scalar/v2-beta
```

### Production with Authorization

```csharp
app.MapOpenApi().RequireAuthorization("ApiDocs");
app.MapScalarApiReference().RequireAuthorization("ApiDocs");
```

## Anti-patterns

### Don't Expose Scalar in Production Without Auth

```csharp
// BAD — anyone can see your API structure
app.MapOpenApi();
app.MapScalarApiReference();

// GOOD — development only
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

### Don't Forget the Security Scheme Transformer

```csharp
// BAD — no auth UI in Scalar because OpenAPI doc has no security schemes
builder.Services.AddOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithPreferredScheme("Bearer"); // Does nothing!
});

// GOOD — register the document transformer first
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

### Don't Leave the Proxy Enabled for Sensitive APIs

```csharp
// BAD — auth headers flow through proxy.scalar.com
app.MapScalarApiReference();

// GOOD — disable proxy for APIs with sensitive data
app.MapScalarApiReference(options =>
{
    options.WithProxy(null);
});
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| API documentation UI | `MapScalarApiReference()` with `MapOpenApi()` |
| Development environment | Default setup with `IsDevelopment()` guard |
| Production API docs | Add `.RequireAuthorization()` to both endpoints |
| Auth testing in dev | `AddHttpAuthentication()` with test tokens |
| Dark theme preference | `.ForceDarkMode()` or `.WithTheme(ScalarTheme.Moon)` |
| Multiple API versions | Multiple `AddOpenApi()` calls — Scalar detects automatically |
| Sensitive APIs | `.WithProxy(null)` to disable external proxy |

## Execution

Configure Scalar as the API documentation UI — setup, theme selection, authentication prefill, proxy settings, or production authorization — wrapping in `IsDevelopment()` by default.

$ARGUMENTS
