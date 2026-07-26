using mediconyx_api.Middleware;
using AspNetCoreRateLimit;

namespace mediconyx_api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseMediconyxPipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // HTTPS redirection is only useful on a developer machine where Kestrel
        // speaks HTTPS directly. In containerized deployments (regardless of
        // ASPNETCORE_ENVIRONMENT value — the dev container also runs as
        // "Development") TLS is terminated at the reverse proxy / load balancer
        // and the app speaks plain HTTP. Calling UseHttpsRedirection() here
        // would only log the noisy "Failed to determine the https port for
        // redirect" warning on every request. DOTNET_RUNNING_IN_CONTAINER is
        // automatically set by the .NET base images.
        var runningInContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!runningInContainer)
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseCors("AllowFrontend");
        // Rate limiting is disabled in the Test environment: the e2e suite fires
        // hundreds of requests per minute from a single IP. appsettings.Test.json
        // cannot clear GeneralRules — JSON config arrays merge by index, not
        // replace — so the base rules from appsettings.json survive there.
        if (!app.Environment.IsEnvironment("Test"))
        {
            app.UseIpRateLimiting();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ModuleEnforcementMiddleware>();
        app.MapControllers();

        return app;
    }
}
