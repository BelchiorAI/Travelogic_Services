using Suppliers.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

// Exposed so WebApplicationFactory<Program> can reach it from the integration tests.
public partial class Program;
