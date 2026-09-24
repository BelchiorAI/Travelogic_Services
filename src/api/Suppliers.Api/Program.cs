using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Suppliers.Api.Endpoints;
using Suppliers.Api.Errors;
using Suppliers.Application;
using Suppliers.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Numbers must be JSON numbers, so the OpenAPI contract (and generated client types) say "number", not "number | string".
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Throw on unreadable requests in every environment, so GlobalExceptionHandler can return ProblemDetails.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString(Suppliers.Infrastructure.DependencyInjection.ConnectionStringName)!,
        name: "sqlserver",
        tags: ["ready"]);

builder.Services.AddApiRateLimiting();

const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
// Local development only: dev servers move to the next free port, so allow any localhost port.
var allowLocalhost = builder.Configuration.GetValue<bool>("Cors:AllowLocalhost");
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .SetIsOriginAllowed(origin =>
        allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
        || (allowLocalhost && Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

// Outermost, so request logs record the final status code after exceptions are turned into ProblemDetails.
app.UseSerilogRequestLogging();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Expected errors (400, 409, and 503 for a switched-off feature) are part of the contract; log the other 5xx.
    SuppressDiagnosticsCallback = context => context.HttpContext.Response.StatusCode
        is < StatusCodes.Status500InternalServerError or StatusCodes.Status503ServiceUnavailable,
});
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();

app.MapOpenApi().WithDocumentPerVersion();
app.MapScalarApiReference(options => options.WithTitle("Supplier API"));

// Live: the process is up (restart if not). Ready: dependencies are reachable (stop sending traffic if not).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

var v1 = app.NewVersionedApi("Suppliers")
    .MapGroup("/api/v{version:apiVersion}")
    .HasApiVersion(1);

v1.MapSupplierEndpoints();
v1.MapFeatureEndpoints();
v1.MapMediaEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can reach it from the integration tests.
public partial class Program;
