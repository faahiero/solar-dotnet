using Serilog;
using Serilog.Events;
using Solar.Infrastructure.Configuration;
using Solar.WebApi.Extensions;
using Solar.WebApi.Logging;

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

// 3. Configuração do Sentry (Error Tracking & Crash Reporting)
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

// 4. Injeção Modular das Camadas da Aplicação (Clean Architecture)
builder.Services
    .AddSolarDatabase(builder.Configuration, builder.Environment)
    .AddSolarApplicationServices()
    .AddSolarSecurityAndAuth(builder.Configuration, builder.Environment)
    .AddSolarIntegrations(builder.Configuration)
    .AddSolarObservability()
    .AddSolarHttpPerformance()
    .AddSolarBackgroundWorkers()
    .AddOpenApi();

var app = builder.Build();
var appStartTime = DateTime.UtcNow;

// 5. Inicialização e Seed do Banco de Dados
app.SeedSolarDatabaseIfEmpty();

// 6. Pipeline de Middlewares e Segurança
app.UseSolarMiddlewarePipeline(app.Environment);

// 7. Mapeamento Modular de Rotas
app.MapSolarEndpoints(appStartTime);

app.Run();

// Helper de tipagem para WebApplicationFactory em testes
public partial class Program { }
