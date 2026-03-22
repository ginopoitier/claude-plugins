# Minimal APIs — .NET Reference

## Endpoint Group Pattern

```csharp
// Presentation/Endpoints/IEndpointGroup.cs
public interface IEndpointGroup
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

## Auto-Discovery Registration

```csharp
// Presentation/Extensions/EndpointExtensions.cs
public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
{
    var endpointGroups = typeof(PresentationAssemblyMarker).Assembly
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpointGroup)))
        .Select(Activator.CreateInstance)
        .Cast<IEndpointGroup>();

    foreach (var group in endpointGroups)
        group.MapEndpoints(app);

    return app;
}
```

## Endpoint Group Example

```csharp
// Presentation/Endpoints/Orders/OrderEndpoints.cs
public sealed class OrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("{id:guid}", GetOrder);
        group.MapPost("", CreateOrder);
        group.MapDelete("{id:guid}/cancel", CancelOrder);
    }

    private static async Task<IResult> GetOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderQuery(id), ct);
        return result.IsFailure ? result.Error!.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateOrderCommand(request.CustomerId, request.Amount), ct);
        return result.IsFailure
            ? result.Error!.ToProblemDetails()
            : Results.CreatedAtRoute("GetOrder", new { id = result.Value }, result.Value);
    }

    private static async Task<IResult> CancelOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new CancelOrderCommand(id), ct);
        return result.IsFailure ? result.Error!.ToProblemDetails() : Results.NoContent();
    }
}
```

## FluentValidation as Endpoint Filter

```csharp
// Presentation/Filters/ValidationFilter.cs
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var param = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (param is null) return await next(ctx);

        var result = await validator.ValidateAsync(param);
        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToDictionary());
        }

        return await next(ctx);
    }
}

// Usage on a route:
group.MapPost("", CreateOrder)
     .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

## Program.cs Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddProblemDetails()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapAllEndpoints();
app.Run();
```

## Global Exception Handler (catches ValidationException → 422)

```csharp
// Presentation/Middleware/GlobalExceptionHandler.cs
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        var (statusCode, title) = exception switch
        {
            ValidationException ve => (StatusCodes.Status422UnprocessableEntity, "Validation failed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var problem = new ProblemDetails { Status = statusCode, Title = title };

        if (exception is ValidationException validationEx)
            problem.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        ctx.Response.StatusCode = statusCode;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
```
