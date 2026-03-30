# Authentication & Authorization — .NET 9 Reference

## Overview

JWT bearer authentication and OIDC provide stateless, standards-based identity for Minimal APIs. Policy-based authorization decouples access rules from endpoint code so that business requirements map directly to named policies rather than scattered `[Authorize(Roles="...")]` attributes. Use this document when wiring up auth in a new service or debugging claim-mapping issues.

## Setup: JWT Bearer Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Authority is the identity server base URL — tokens issued by it are trusted
            options.Authority = config["Auth:Authority"];

            // Audience must match the API resource registered in your identity server
            options.Audience = config["Auth:Audience"];

            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Always validate lifetime; skipping this opens replay attacks
                ValidateLifetime = true,

                // Clock skew: allow 30 s drift between server clocks (default is 5 min — too generous)
                ClockSkew = TimeSpan.FromSeconds(30),

                // Validate issuer to prevent tokens from a different IdP being accepted
                ValidateIssuer = true,

                // Validate audience to prevent tokens meant for another API
                ValidateAudience = true,

                // Map the "sub" claim to ClaimTypes.NameIdentifier so User.Identity.Name resolves correctly
                NameClaimType = "sub",
                RoleClaimType = "roles"
            };

            // Forward auth failures as ProblemDetails rather than plain 401 HTML
            options.Events = new JwtBearerEvents
            {
                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/problem+json";
                    var problem = new ProblemDetails
                    {
                        Status = 401,
                        Title = "Unauthorized",
                        Detail = "A valid bearer token is required."
                    };
                    return ctx.Response.WriteAsJsonAsync(problem);
                }
            };
        });

    services.AddAuthorization();
    return services;
}
```

## Pattern: Policy-Based Authorization

Define policies once at startup; reference them by name at the endpoint. This keeps authorization logic out of handler code.

```csharp
// Infrastructure/Auth/AuthorizationPolicies.cs
public static class AuthorizationPolicies
{
    public const string CanManageOrders  = "CanManageOrders";
    public const string IsAdmin          = "IsAdmin";
    public const string IsVerifiedBuyer  = "IsVerifiedBuyer";
}

// Infrastructure/DependencyInjection.cs (inside AddInfrastructure)
services.AddAuthorization(options =>
{
    // Role-based: the "roles" claim must contain "admin"
    options.AddPolicy(AuthorizationPolicies.IsAdmin, policy =>
        policy.RequireRole("admin"));

    // Claim-based: user must have email_verified=true AND belong to the right tenant
    options.AddPolicy(AuthorizationPolicies.IsVerifiedBuyer, policy =>
        policy.RequireClaim("email_verified", "true")
              .RequireClaim("tenant_id"));          // value checked by requirement below

    // Custom requirement: orders can only be managed by users with "orders:write" scope
    options.AddPolicy(AuthorizationPolicies.CanManageOrders, policy =>
        policy.Requirements.Add(new ScopeRequirement("orders:write")));
});

// Register handler — MUST be in DI or the policy silently fails
services.AddSingleton<IAuthorizationHandler, ScopeRequirementHandler>();
```

## Pattern: Custom Authorization Requirement

Use when a claim value needs non-trivial validation that a simple `RequireClaim` cannot express.

```csharp
// Infrastructure/Auth/ScopeRequirement.cs
public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

// Infrastructure/Auth/ScopeRequirementHandler.cs
public sealed class ScopeRequirementHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        // The "scope" claim may contain multiple space-delimited values (OAuth 2.0 spec)
        var scopeClaim = context.User.FindFirst("scope")?.Value ?? string.Empty;
        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (scopes.Contains(requirement.Scope))
            context.Succeed(requirement);
        // Do NOT call context.Fail() unless you want to short-circuit other handlers

        return Task.CompletedTask;
    }
}
```

## Pattern: Resource-Based Authorization

Use when the authorization decision depends on the resource being accessed (e.g., "can this user edit this order?"), not just the user's claims.

```csharp
// Domain/Orders/OrderAuthorizationHandler.cs
public sealed class OrderEditRequirement : IAuthorizationRequirement { }

public sealed class OrderAuthorizationHandler
    : AuthorizationHandler<OrderEditRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderEditRequirement requirement,
        Order resource)
    {
        // Admins can always edit; buyers can only edit their own orders
        if (context.User.IsInRole("admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst("sub")?.Value;
        if (userId is not null && resource.CustomerId.Value.ToString() == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Usage in a command handler — inject IAuthorizationService
public sealed class UpdateOrderHandler(
    AppDbContext db,
    IAuthorizationService authz,
    IHttpContextAccessor http)
    : IRequestHandler<UpdateOrderCommand, Result>
{
    public async Task<Result> Handle(UpdateOrderCommand cmd, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([new OrderId(cmd.OrderId)], ct);
        if (order is null) return OrderErrors.NotFound;

        // Resource-based check: pass the actual entity
        var result = await authz.AuthorizeAsync(
            http.HttpContext!.User,
            order,
            new OrderEditRequirement());

        if (!result.Succeeded)
            return OrderErrors.Forbidden;

        order.Update(cmd.NewStatus);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

## Pattern: OIDC with Duende IdentityServer / Azure AD

For browser-facing apps or when you need refresh tokens, add OIDC on top of JWT bearer.

```csharp
// For a BFF (Backend For Frontend) that issues cookies to the browser
// and validates tokens internally before forwarding to downstream APIs
builder.Services
    .AddAuthentication(options =>
    {
        // Cookie is the primary scheme for the browser session
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // OIDC is the challenge scheme — redirects to the IdP
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(cookie =>
    {
        cookie.Cookie.HttpOnly = true;     // XSS protection
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
        cookie.SlidingExpiration = true;
        cookie.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddOpenIdConnect(oidc =>
    {
        oidc.Authority    = builder.Configuration["Auth:Authority"];
        oidc.ClientId     = builder.Configuration["Auth:ClientId"];
        oidc.ClientSecret = builder.Configuration["Auth:ClientSecret"]; // store in secrets/vault
        oidc.ResponseType = "code";      // Authorization Code Flow — never use implicit
        oidc.SaveTokens   = true;        // needed to access tokens for downstream API calls
        oidc.Scope.Add("openid");
        oidc.Scope.Add("profile");
        oidc.Scope.Add("orders:write");  // API scope

        // Map IdP claims to .NET claim types
        oidc.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        oidc.ClaimActions.MapJsonKey("tenant_id", "tid");

        // Disable the default claim filtering so "roles" survives the pipeline
        oidc.MapInboundClaims = false;
    });
```

## Pattern: Applying Auth to Endpoint Groups

```csharp
// Presentation/Endpoints/Orders/OrderEndpoints.cs
public sealed class OrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            // All endpoints in this group require authentication
            .RequireAuthorization();

        group.MapGet("{id:guid}", GetOrder);

        // Override at endpoint level for finer-grained policy
        group.MapPost("", CreateOrder)
             .RequireAuthorization(AuthorizationPolicies.CanManageOrders);

        group.MapDelete("{id:guid}", DeleteOrder)
             .RequireAuthorization(AuthorizationPolicies.IsAdmin);

        // Public endpoint inside an otherwise protected group
        group.MapGet("public/featured", GetFeatured)
             .AllowAnonymous();
    }
}
```

## Pattern: Extracting the Current User

Avoid `IHttpContextAccessor` in deep domain code. Push user identity resolution to the application layer boundary via a service interface.

```csharp
// Application/Abstractions/ICurrentUser.cs
public interface ICurrentUser
{
    Guid   UserId    { get; }
    string Email     { get; }
    bool   IsAdmin   { get; }
    bool   IsAuthenticated { get; }
}

// Infrastructure/Auth/CurrentUser.cs
public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? User => http.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    // "sub" is the standard OIDC subject claim
    public Guid UserId => Guid.Parse(
        User?.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("No authenticated user."));

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public bool IsAdmin => User?.IsInRole("admin") ?? false;
}

// Registration
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUser, CurrentUser>();
```

## Anti-patterns

### Don't store secrets in appsettings.json

```csharp
// BAD — client secrets and signing keys checked into source control
// appsettings.json:
// "Auth": { "ClientSecret": "super-secret-key-123" }

// GOOD — use user secrets locally, Azure Key Vault / AWS Secrets Manager in production
// appsettings.json only holds non-sensitive config:
// "Auth": { "Authority": "https://login.example.com", "Audience": "my-api" }

// Program.cs — ASP.NET Core secrets pipeline handles the rest:
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["Azure:KeyVaultUri"]!),
    new DefaultAzureCredential());
```

### Don't validate tokens manually

```csharp
// BAD — hand-rolled JWT parsing skips standard security checks (lifetime, issuer, signature algorithm)
app.MapGet("/secure", (HttpContext ctx) =>
{
    var token = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);          // no validation!
    var userId = jwt.Claims.First(c => c.Type == "sub").Value;
    return Results.Ok(userId);
});

// GOOD — let the middleware validate; access the already-validated ClaimsPrincipal
app.MapGet("/secure", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst("sub")!.Value;
    return Results.Ok(userId);
}).RequireAuthorization();
```

### Don't use roles as the sole authorization mechanism

```csharp
// BAD — roles embedded in tokens are hard to change without re-issuing tokens;
//       business rules leak into identity claims
options.AddPolicy("CanRefund", p => p.RequireRole("finance-team"));

// GOOD — use fine-grained scopes for API-level decisions;
//         keep roles for coarse identity grouping only
options.AddPolicy("CanRefund", p =>
    p.Requirements.Add(new ScopeRequirement("payments:refund")));
```

## Reference

**NuGet Packages:**
```
Microsoft.AspNetCore.Authentication.JwtBearer       9.0.*
Microsoft.AspNetCore.Authentication.OpenIdConnect   9.0.*
Microsoft.AspNetCore.Authorization                  9.0.*
System.IdentityModel.Tokens.Jwt                     8.*
Azure.Extensions.AspNetCore.Configuration.Secrets   1.*
Azure.Identity                                      1.*
```

**Configuration (appsettings.json):**
```json
{
  "Auth": {
    "Authority": "https://your-idp.example.com",
    "Audience":  "your-api-resource-identifier",
    "ClientId":  "your-client-id"
  }
}
```

**Middleware order (Program.cs) — must be exact:**
```csharp
app.UseAuthentication();   // 1. resolve identity
app.UseAuthorization();    // 2. enforce policies — always after authentication
```
