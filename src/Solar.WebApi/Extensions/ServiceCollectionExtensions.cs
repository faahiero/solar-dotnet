using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Solar.Application.Administration;
using Solar.Application.Auth;
using Solar.Application.Common;
using Solar.Application.Common.Mediator;
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
using Solar.Infrastructure.Caching;
using Solar.Infrastructure.Configuration;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Integrations.BigBlueButton;
using Solar.Infrastructure.Integrations.Sigaa;
using Solar.Infrastructure.Persistence;
using Solar.Infrastructure.Reports;
using Solar.WebApi.Middlewares;
using Solar.WebApi.Validators;

namespace Solar.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSolarDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddSingleton<Solar.Infrastructure.Persistence.Interceptors.AuditingInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString) && !environment.IsEnvironment("Testing"))
        {
            var formattedConn = FormatNpgsqlConnectionString(connectionString);
            services.AddDbContext<SolarDbContext>((sp, options) =>
            {
                options.UseNpgsql(formattedConn, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    npgsql.CommandTimeout(30);
                });
                options.AddInterceptors(sp.GetRequiredService<Solar.Infrastructure.Persistence.Interceptors.AuditingInterceptor>());
            });
        }
        else
        {
            services.AddDbContext<SolarDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("SolarDb");
                options.AddInterceptors(sp.GetRequiredService<Solar.Infrastructure.Persistence.Interceptors.AuditingInterceptor>());
            });
        }

        services.AddScoped<ISolarAuthDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
        services.AddScoped<IBlacklistDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
        return services;
    }

    public static IServiceCollection AddSolarApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ISolarCacheService, SolarMemoryCacheService>();

        // Serviços de Domínio
        services.AddSingleton<GradingCalculationService>();
        services.AddSingleton<ExamScoringService>();
        services.AddSingleton<AllocationTagScopeService>();
        services.AddSingleton<DiscussionTreeService>();
        services.AddSingleton<DisciplineImportService>();
        services.AddSingleton<GroupAssignmentService>();
        services.AddSingleton<InternalMessagingService>();
        services.AddSingleton<UserBatchImportService>();
        services.AddScoped<BlacklistService>();
        services.AddSingleton<PasswordPolicyService>();
        services.AddHttpClient<CepLookupService>();
        services.AddSingleton<IEmailNotificationService, ConsoleEmailNotificationService>();
        services.AddScoped<PasswordResetService>();

        // Casos de Uso (Use Cases)
        services.AddScoped<CalculateStudentGradesUseCase>();
        services.AddScoped<AuthenticateUserUseCase>();
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<PasswordHasher<User>>();
        services.AddScoped<IPasswordHasher<User>, DeviseLegacyPasswordHasher<User>>();

        // Despacho e Manipuladores de Eventos de Domínio
        services.AddScoped<Solar.Application.Common.IDomainEventDispatcher, Solar.Infrastructure.Events.DomainEventDispatcher>();
        services.AddScoped<Solar.Application.Events.AuditDomainEventHandler>();
        services.AddScoped<Solar.Application.Common.IDomainEventHandler<Solar.Domain.Events.GradeUpdatedDomainEvent>>(sp => sp.GetRequiredService<Solar.Application.Events.AuditDomainEventHandler>());
        services.AddScoped<Solar.Application.Common.IDomainEventHandler<Solar.Domain.Events.UserBlacklistedDomainEvent>>(sp => sp.GetRequiredService<Solar.Application.Events.AuditDomainEventHandler>());
        services.AddScoped<Solar.Application.Common.IDomainEventHandler<Solar.Domain.Events.ExamAttemptCompletedDomainEvent>>(sp => sp.GetRequiredService<Solar.Application.Events.AuditDomainEventHandler>());

        // Mediator Nativo C# (.NET 10) e CQRS Handlers
        services.AddSolarMediator(typeof(Solar.Application.Auth.Commands.AuthenticateUserCommandHandler).Assembly);

        // Validadores do FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateGroupRequestValidator>();

        return services;
    }

    public static IServiceCollection AddSolarSecurityAndAuth(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? 
                        configuration["Jwt__Secret"] ?? 
                        configuration["JWT_SECRET"];

        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException("Configuração Insegura: A variável de ambiente JWT_SECRET é estritamente obrigatória em ambiente de produção.");
            }
            jwtSecret = "SolarLmsSecretKeyUfcVirtualEnterprise2026SecureJwtTokenSignatureKey123!";
        }

        var jwtIssuer = configuration["Jwt:Issuer"] ?? "SolarLms.UfcVirtual";
        var jwtAudience = configuration["Jwt:Audience"] ?? "SolarLms.Clients";

        var jwtConfig = new JwtTokenConfig
        {
            SecretKey = jwtSecret,
            Issuer = jwtIssuer,
            Audience = jwtAudience
        };
        services.AddSingleton(jwtConfig);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ISolarAuthorizationService, SolarAuthorizationService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                    {
                        context.Token = accessToken;
                    }
                    else if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("solar_access_token", out var cookieToken))
                    {
                        context.Token = cookieToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.IsInRole("Administrador") ||
                    ctx.User.HasClaim(c => c.Type == "role" && (c.Value == "admin" || c.Value == "Admin"))));

            options.AddPolicy("TeacherPolicy", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("teacher") ||
                    ctx.User.IsInRole("Teacher") ||
                    ctx.User.IsInRole("Professor") ||
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(c => c.Type == "role" && (c.Value == "teacher" || c.Value == "admin"))));

            options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
        });

        // Rate Limiting Nativo do ASP.NET Core (.NET 10)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("AuthLimiter", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddSlidingWindowLimiter("GeneralApiLimiter", opt =>
            {
                opt.PermitLimit = 300;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });
        });

        return services;
    }

    public static IServiceCollection AddSolarIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new BigBlueButtonServerConfig
        {
            ServerUrl = configuration["BigBlueButton:ServerUrl"] ?? "https://bbb.virtual.ufc.br/bigbluebutton",
            SharedSecret = configuration["BigBlueButton:SharedSecret"] ?? "solar_secret"
        });
        services.AddSingleton<BigBlueButtonClient>();

        // Clientes HTTP com políticas de timeout e resiliência
        services.AddHttpClient<ISigaaAcademicService, SigaaAcademicClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<CepLookupService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
        });

        services.AddSingleton<IAcademicReportService, AcademicPdfReportService>();
        return services;
    }

    public static IServiceCollection AddSolarObservability(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<SolarGlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddSolarHttpPerformance(this IServiceCollection services)
    {
        services.AddLocalization();
        services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[] { "pt-BR", "pt", "en-US", "en" };
            options.SetDefaultCulture("pt-BR")
                   .AddSupportedCultures(supportedCultures)
                   .AddSupportedUICultures(supportedCultures);
        });

        services.AddSignalR();

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromSeconds(30)));
            options.AddPolicy("AcademicPolicy", policy => policy.Expire(TimeSpan.FromMinutes(2)).Tag("academic"));
            options.AddPolicy("StaticCatalogPolicy", policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("catalog"));
        });

        services.AddResponseCompression(options =>
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

        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        return services;
    }

    public static IServiceCollection AddSolarBackgroundWorkers(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue>(new DefaultBackgroundTaskQueue(200));
        services.AddHostedService<QueuedHostedService>();
        services.AddHostedService<AcademicMaintenanceWorker>();
        return services;
    }

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
