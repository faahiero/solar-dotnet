using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Endpoints;
using Solar.WebApi.Middlewares;

namespace Solar.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseSolarMiddlewarePipeline(this WebApplication app, IWebHostEnvironment environment)
    {
        // Tratamento Global Padronizado de Erros (RFC 7807)
        app.UseExceptionHandler();

        if (environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Logging Estruturado de Requisições HTTP com Serilog
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                var path = httpContext.Request.Path.Value ?? "";
                if (path.StartsWith("/api/v1/admin/logs", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/livez", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/readyz", StringComparison.OrdinalIgnoreCase))
                {
                    return LogEventLevel.Verbose;
                }

                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };
        });

        // Cabeçalhos de Segurança HTTP Modernos (OWASP / MEC)
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Proteção Criptográfica de Amarração de Dispositivo / IP (Device Fingerprint)
        app.UseMiddleware<DeviceFingerprintMiddleware>();

        // Suporte a Internacionalização (i18n): pt-BR e en-US
        app.UseRequestLocalization();

        // Compressão Dinâmica de Resposta HTTP (Brotli/Gzip)
        app.UseResponseCompression();

        // Rate Limiting para Proteção contra DoS e Força Bruta
        app.UseRateLimiter();

        // Habilitar Output Caching para respostas HTTP em cache no servidor
        app.UseOutputCache();

        // Servir o painel web interativo estático (wwwroot)
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Autenticação e Autorização Criptográfica (JWT Bearer + Cookies HttpOnly)
        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware Anti-Fraude de Bloqueio em Provas
        app.UseMiddleware<ExamLockoutMiddleware>();

        return app;
    }

    public static WebApplication MapSolarEndpoints(this WebApplication app, DateTime appStartTime)
    {
        // OpenAPI e Documentação Interativa de Rotas (Swagger / Scalar)
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Solar LMS 2.0 API - UFC Virtual");
            options.WithTheme(ScalarTheme.BluePlanet);
        });

        // ----------------------------------------------------
        // Mapeamento Modular de Endpoints (Clean Architecture)
        // ----------------------------------------------------
        app.MapDiagnosticEndpoints(appStartTime);
        app.MapAuthEndpoints(app.Environment);
        app.MapAcademicEndpoints();
        app.MapCommunicationEndpoints();
        app.MapAssessmentEndpoints();
        app.MapReportEndpoints();
        app.MapAdminEndpoints();

        // Suporte a rotas profundas do SPA React (evita 404 ao recarregar a página)
        app.MapFallbackToFile("index.html");

        return app;
    }

    public static void SeedSolarDatabaseIfEmpty(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
        try
        {
            db.Database.EnsureCreated();
            if (!db.Users.Any())
            {
                db.Users.AddRange(
                    new User
                    {
                        Username = "aluno1",
                        Nick = "Aluno",
                        Name = "Aluno 1 (Demonstração)",
                        Email = "aluno1@solar.ufc.br",
                        Cpf = "12345678900",
                        EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("123456"),
                        Active = true
                    },
                    new User
                    {
                        Username = "alunoteste",
                        Nick = "Aluno",
                        Name = "Aluno Demonstrativo",
                        Email = "aluno@solar.ufc.br",
                        Cpf = "12345678901",
                        EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("senhadoteste123"),
                        Active = true,
                        Integrated = true,
                        Selfregistration = false
                    },
                    new User
                    {
                        Username = "prof.fabricio",
                        Nick = "Fabrício",
                        Name = "Prof. Fabrício Silva",
                        Email = "fabricio@virtual.ufc.br",
                        Cpf = "99988877766",
                        EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("solar123"),
                        Active = true
                    },
                    new User
                    {
                        Username = "prof",
                        Nick = "Professor",
                        Name = "Prof. Titular UAB",
                        Email = "prof@solar.ufc.br",
                        Cpf = "99988877700",
                        EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("123456"),
                        Active = true
                    }
                );
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Aviso na verificação/inicialização de banco de dados.");
        }
    }
}
