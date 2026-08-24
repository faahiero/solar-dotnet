using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public static class DiagnosticEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder app, DateTime appStartTime)
    {
        // Redirecionamento Swagger -> Scalar
        app.MapGet("/swagger", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

        // Health Checks & Probes de Orquestração
        app.MapGet("/health", async (SolarDbContext db) =>
        {
            bool dbOk = false;
            try
            {
                dbOk = await db.Database.CanConnectAsync();
            }
            catch { }

            var memoryUsageMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
            var uptime = DateTime.UtcNow - appStartTime;

            return Results.Ok(new
            {
                Status = dbOk ? "Healthy" : "Degraded",
                Database = dbOk ? "Connected" : "Disconnected",
                System = "Solar LMS Core (.NET 10)",
                MemoryUsageMB = memoryUsageMb,
                Uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                UptimeSeconds = (long)uptime.TotalSeconds,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("HealthCheck")
        .WithSummary("Verifica a integridade do serviço, telemetria de memória e banco de dados");

        app.MapGet("/healthz", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
            .WithName("Healthz")
            .WithSummary("Verificação geral de integridade (Health Probe)");

        app.MapGet("/livez", () => Results.Ok(new { Status = "Live", Timestamp = DateTime.UtcNow }))
            .WithName("Livez")
            .WithSummary("Liveness Probe para orquestradores");

        app.MapGet("/readyz", async (SolarDbContext db) =>
        {
            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                return canConnect
                    ? Results.Ok(new { Status = "Ready", Database = "Connected", Timestamp = DateTime.UtcNow })
                    : Results.Json(new { Status = "Not Ready", Database = "Disconnected" }, statusCode: 503);
            }
            catch (Exception ex)
            {
                return Results.Json(new { Status = "Error", Message = ex.Message }, statusCode: 503);
            }
        })
        .WithName("Readyz")
        .WithSummary("Readiness Probe para tráfego");

        // Consulta de Idiomas e Culturas Suportadas (i18n)
        app.MapGet("/api/v1/locales", (HttpContext httpContext) =>
        {
            var reqCulture = httpContext.Features.Get<IRequestCultureFeature>();
            var currentCulture = reqCulture?.RequestCulture.UICulture.Name ?? CultureInfo.CurrentUICulture.Name;
            return Results.Ok(new
            {
                CurrentCulture = currentCulture,
                SupportedCultures = new[]
                {
                    new { Code = "pt-BR", Name = "Português (Brasil)", Flag = "🇧🇷", IsDefault = true },
                    new { Code = "en-US", Name = "English (USA)", Flag = "🇺🇸", IsDefault = false }
                }
            });
        })
        .WithName("GetLocales")
        .WithSummary("Retorna os idiomas e culturas suportadas pelo Solar LMS (.NET 10)")
        .CacheOutput(p => p.SetVaryByHeader("Accept-Language").SetVaryByQuery("culture", "locale", "ui-culture"));

        return app;
    }
}
