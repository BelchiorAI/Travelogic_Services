var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

// Exposed so WebApplicationFactory<Program> can reach it from the integration tests.
public partial class Program;
