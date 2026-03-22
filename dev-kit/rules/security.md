# Rule: Security

## DO
- Use **JWT bearer authentication** with `Microsoft.AspNetCore.Authentication.JwtBearer`
- Store secrets in **User Secrets** (dev) and **Azure Key Vault / environment variables** (prod)
- Use `[Authorize]` on all endpoints that require authentication — default to requiring auth
- Validate and sanitize all **user inputs** — use FluentValidation at the application boundary
- Use **parameterized queries** everywhere — EF Core LINQ is safe; never build SQL by concatenation
- Use `CryptographicOperations.FixedTimeEquals` for token/secret comparisons (prevent timing attacks)
- Enable **HTTPS only** — redirect HTTP to HTTPS, use HSTS
- Apply **CORS** restrictively — list allowed origins explicitly, never use `AllowAnyOrigin` + `AllowCredentials`
- Use `IDataProtectionProvider` for protecting data at rest (e.g., cookies, tokens)
- Add **rate limiting** to authentication endpoints and public APIs
- Log **authentication failures** and **authorization denials** at `Warning` level with context

## DON'T
- Don't hardcode connection strings, API keys, or passwords — ever
- Don't log sensitive data: passwords, tokens, credit cards, PII
- Don't use `MD5` or `SHA1` for password hashing — use `BCrypt` or `Argon2` via `Microsoft.AspNetCore.Identity`
- Don't expose internal stack traces in API responses — use ProblemDetails without stack
- Don't use `string.Format` or interpolation to build SQL — always parameterized
- Don't use `Roles` for fine-grained authorization — use **policy-based authorization**
- Don't disable SSL certificate validation in production code
- Don't store sensitive data in cookies without encryption
- Don't trust `X-Forwarded-For` without configuring `ForwardedHeaders` middleware properly

## Security Headers (add via middleware)
```csharp
app.UseHsts();
app.UseHttpsRedirection();
// Custom headers middleware:
// X-Content-Type-Options: nosniff
// X-Frame-Options: DENY
// X-XSS-Protection: 1; mode=block
// Referrer-Policy: no-referrer
// Content-Security-Policy: default-src 'self'
```

## Secrets Management
```bash
# Development — User Secrets
dotnet user-secrets init --project src/YourApp.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "Jwt:Secret" "..."

# Production — environment variables (12-factor)
# CONNECTIONSTRINGS__DEFAULTCONNECTION=...
# JWT__SECRET=...
```
