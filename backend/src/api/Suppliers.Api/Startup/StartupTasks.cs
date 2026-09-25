using Microsoft.Extensions.Diagnostics.HealthChecks;
using Suppliers.Infrastructure;

namespace Suppliers.Api.Startup;

/// <summary>
/// Runs slow startup work (database migrations, media bucket setup) after the web server is listening,
/// so hosts that wait for an open port (e.g. Render) don't give up while a paused database wakes up.
/// Requests that arrive meanwhile wait for it to finish; /health/ready reports "not ready" until then.
/// </summary>
internal sealed class StartupTasks
{
    public Task Completion { get; private set; } = Task.CompletedTask;

    public void Start(WebApplication app) => Completion = RunAsync(app);

    private static async Task RunAsync(WebApplication app)
    {
        // Let the host finish starting (and open the port) before doing anything slow.
        await Task.Yield();
        try
        {
            if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
                await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);

            await app.Services.EnsureMediaBucketAsync(app.Lifetime.ApplicationStopping);
            app.Logger.LogInformation("Startup tasks finished");
        }
        catch (Exception ex)
        {
            // Stop rather than run half-configured; the host restarts the service and the log says why.
            app.Logger.LogCritical(ex, "Startup tasks failed; stopping the application");
            app.Lifetime.StopApplication();
            throw;
        }
    }
}

internal static class StartupTasksExtensions
{
    public static IServiceCollection AddStartupTasks(this IServiceCollection services, IHealthChecksBuilder healthChecks)
    {
        services.AddSingleton<StartupTasks>();
        healthChecks.AddCheck<StartupHealthCheck>("startup", tags: ["ready"]);
        return services;
    }

    /// <summary>Starts the startup tasks and makes requests wait for them (health checks excepted).</summary>
    public static WebApplication UseStartupTasks(this WebApplication app)
    {
        var tasks = app.Services.GetRequiredService<StartupTasks>();
        tasks.Start(app);

        app.Use(async (context, next) =>
        {
            if (!tasks.Completion.IsCompleted && !context.Request.Path.StartsWithSegments("/health"))
                await tasks.Completion;
            await next(context);
        });
        return app;
    }
}

internal sealed class StartupHealthCheck(StartupTasks tasks) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
        Task.FromResult(tasks.Completion.IsCompletedSuccessfully
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy(tasks.Completion.IsFaulted ? "Startup tasks failed." : "Still starting up."));
}
