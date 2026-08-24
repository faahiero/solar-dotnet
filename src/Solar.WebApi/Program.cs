using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Application.Auth;
using Solar.Application.Common;
using Solar.Application.Grading;
using Solar.Application.Integrations.Sigaa;
using Solar.Application.Reports;
using Solar.Domain.Academic;
using Solar.Domain.Administration;
using Solar.Domain.Assessments;
using Solar.Domain.Communication;
using Solar.Domain.Discussions;
using Solar.Domain.Entities;
using Solar.Domain.Grading;
using Solar.Infrastructure.Background;
using Solar.Infrastructure.Configuration;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Integrations.BigBlueButton;
using Solar.Infrastructure.Integrations.Sigaa;
using Solar.Infrastructure.Persistence;
using Solar.Infrastructure.Caching;
using Solar.WebApi.Logging;
using Solar.Infrastructure.Reports;
using Solar.WebApi.Hubs;
using Solar.WebApi.Middlewares;
using Solar.WebApi.Endpoints;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// 1. Carrega variáveis de ambiente do arquivo .env (se presente) para desenvolvimento local
DotEnvLoader.Load();

// 2. Configuração do Serilog (Structured Logging + Dashboard Embutido + Seq)
var seqServerUrl = Environment.GetEnvironmentVariable("SEQ_SERVER_URL") ?? 
                   Environment.GetEnvironmentVariable("Seq__ServerUrl");

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Solar.LMS")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Sink(new SolarLogSink());

if (!string.IsNullOrWhiteSpace(seqServerUrl))
{
    loggerConfig.WriteTo.Seq(seqServerUrl);
}

Log.Logger = loggerConfig.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Configuration.AddEnvironmentVariables();

// 3. Configuração do Sentry (Error Tracking & Crash Reporting) se DSN fornecido
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrWhiteSpace(sentryDsn) && !builder.Environment.IsEnvironment("Testing"))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.TracesSampleRate = 1.0;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
    });
}

// Configuração do DbContext (PostgreSQL em produção ou InMemory para testes)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    var formattedConn = FormatNpgsqlConnectionString(connectionString);
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseNpgsql(formattedConn, npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
            npgsql.CommandTimeout(30);
        }));
}
else
{
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseInMemoryDatabase("SolarDb"));
}

// Injeção de dependências dos serviços de Domínio e Aplicação
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISolarCacheService, SolarMemoryCacheService>();
builder.Services.AddScoped<ISolarAuthDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
builder.Services.AddScoped<IBlacklistDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
builder.Services.AddSingleton<GradingCalculationService>();
builder.Services.AddSingleton<ExamScoringService>();
builder.Services.AddSingleton<AllocationTagScopeService>();
builder.Services.AddSingleton<DiscussionTreeService>();
builder.Services.AddSingleton<DisciplineImportService>();
builder.Services.AddSingleton<GroupAssignmentService>();
builder.Services.AddSingleton<InternalMessagingService>();
builder.Services.AddSingleton<UserBatchImportService>();
builder.Services.AddScoped<BlacklistService>();
builder.Services.AddSingleton<PasswordPolicyService>();
builder.Services.AddHttpClient<CepLookupService>();
builder.Services.AddSingleton<IEmailNotificationService, ConsoleEmailNotificationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<CalculateStudentGradesUseCase>();
builder.Services.AddScoped<AuthenticateUserUseCase>();
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<IPasswordHasher<User>, DeviseLegacyPasswordHasher<User>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISolarAuthorizationService, SolarAuthorizationService>();

// Integrações Externas (BigBlueButton e SIGAA)
builder.Services.AddSingleton(new BigBlueButtonServerConfig
{
    ServerUrl = builder.Configuration["BigBlueButton:ServerUrl"] ?? "https://bbb.virtual.ufc.br/bigbluebutton",
    SharedSecret = builder.Configuration["BigBlueButton:SharedSecret"] ?? "solar_secret"
});
builder.Services.AddSingleton<BigBlueButtonClient>();
builder.Services.AddSingleton<ISigaaAcademicService, SigaaAcademicClient>();
builder.Services.AddSingleton<IAcademicReportService, AcademicPdfReportService>();

// Filas e Processamento Assíncrono em Segundo Plano (Substitui DelayedJob / Rufus)
builder.Services.AddSingleton<IBackgroundTaskQueue>(new DefaultBackgroundTaskQueue(200));
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<AcademicMaintenanceWorker>();

// Suporte a Internacionalização (i18n) em Português e Inglês
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "pt-BR", "pt", "en-US", "en" };
    options.SetDefaultCulture("pt-BR")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

// SignalR para Chat e Notificações em tempo real
builder.Services.AddSignalR();

// Output Caching do ASP.NET Core (Respostas HTTP Instantâneas na Memória RAM)
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromSeconds(30)));
    options.AddPolicy("AcademicPolicy", policy => policy.Expire(TimeSpan.FromMinutes(2)).Tag("academic"));
    options.AddPolicy("StaticCatalogPolicy", policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("catalog"));
});

// Compressão de Resposta HTTP em Tempo Real (Brotli / Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "text/plain",
        "text/css",
        "application/javascript",
        "image/svg+xml"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Rate Limiting Nativo do ASP.NET Core (.NET 10)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política de proteção contra força bruta em autenticação
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 30; // 30 tentativas por minuto
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Política geral para APIs
    options.AddSlidingWindowLimiter("GeneralApiLimiter", opt =>
    {
        opt.PermitLimit = 300; // 300 requisições por minuto
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

// OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();
var appStartTime = DateTime.UtcNow;

// Inicialização de schema e usuários demo se o banco for novo/vazio
using (var scope = app.Services.CreateScope())
{
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

// OpenAPI e Documentação Interativa de Rotas (Swagger / Scalar)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Solar LMS 2.0 API - UFC Virtual");
    options.WithTheme(ScalarTheme.BluePlanet);
});

if (app.Environment.IsDevelopment())
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

// Middleware Anti-Fraude de Bloqueio em Provas
app.UseMiddleware<ExamLockoutMiddleware>();

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

app.Run();

// Helper e Configuração Estática
public partial class Program
{
    public static string FormatNpgsqlConnectionString(string connStr)
    {
        if (string.IsNullOrWhiteSpace(connStr)) return connStr;
        if (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(connStr);
                var userInfo = uri.UserInfo.Split(':');
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Keepalive=30;Pooling=true;MinPoolSize=1;MaxPoolSize=25;";
            }
            catch
            {
                return connStr;
            }
        }
        return connStr;
    }
}
