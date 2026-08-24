using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Application.Common;
using Solar.Application.Integrations.Sigaa;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CheckPasswordPolicyRequest(string Password, string? Username = null, string? Cpf = null);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment environment)
    {
        // Autenticação com suporte aos hashes legados Devise, Account Lockout e 2FA
        app.MapPost("/api/v1/auth/login", async (
            LoginRequest request,
            AuthenticateUserUseCase authUseCase,
            IJwtTokenService jwtTokenService,
            HttpContext httpContext) =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var enrichedRequest = request with { RemoteIp = clientIp };
            var result = await authUseCase.ExecuteAsync(enrichedRequest);

            if (!result.Success || result.User == null)
            {
                return Results.BadRequest(new { Success = false, Message = result.Message ?? "Usuário ou senha inválidos." });
            }

            // Gera token JWT criptográfico amarrado com Device Fingerprint
            var userSummary = new UserSummaryDto(
                Id: result.User.Id,
                Username: result.User.Username,
                Name: result.User.Name,
                Email: result.User.Email,
                Cpf: result.User.Cpf,
                Role: result.User.ProfileTypes == 16 ? "admin" : result.User.ProfileTypes == 4 ? "teacher" : "student"
            );
            var tokenResponse = jwtTokenService.GenerateToken(userSummary, expirationSeconds: 86400, clientIp: clientIp, userAgent: userAgent);
            var token = tokenResponse.AccessToken;

            // Emite Cookie blindado HttpOnly + Secure + SameSite=Strict
            httpContext.Response.Cookies.Append("solar_access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24),
                Path = "/"
            });

            var responseWithJwt = result with { Token = token };
            return Results.Ok(responseWithJwt);
        })
        .RequireRateLimiting("AuthLimiter")
        .WithName("Login")
        .WithSummary("Autentica o usuário com suporte a migração de hashes legados, cookies blindados HttpOnly e Device Fingerprint");

        // Encerramento de Sessão e Revogação de Credencial (Logout Seguro)
        app.MapPost("/api/v1/auth/logout", (
            HttpContext httpContext,
            IJwtTokenService jwtTokenService) =>
        {
            httpContext.Response.Cookies.Delete("solar_access_token", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            });

            string? token = null;
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
            else if (httpContext.Request.Cookies.TryGetValue("solar_access_token", out var cookieToken))
            {
                token = cookieToken;
            }

            if (!string.IsNullOrEmpty(token))
            {
                jwtTokenService.RevokeToken(token);
            }

            return Results.Ok(new { Success = true, Message = "Sessão encerrada com sucesso e credencial revogada." });
        })
        .WithName("Logout")
        .WithSummary("Encerra a sessão, remove cookies blindados e revoga o token no servidor");

        // Consulta de CEP (ViaCEP / Busca CEP)
        app.MapGet("/api/v1/cep/{cep}", async (string cep, CepLookupService cepService) =>
        {
            var result = await cepService.LookupAsync(cep);
            return Results.Ok(result);
        })
        .WithName("LookupCep")
        .WithSummary("Consulta dados de endereço a partir do CEP para preenchimento automático");

        // Verificação de Força e Entropia da Senha (NIST SP 800-63B)
        app.MapPost("/api/v1/auth/password-policy/check", (CheckPasswordPolicyRequest request, PasswordPolicyService policyService) =>
        {
            var result = policyService.ValidatePassword(request.Password, request.Username, request.Cpf);
            return Results.Ok(result);
        })
        .WithName("CheckPasswordPolicy")
        .WithSummary("Verifica a força e conformidade da senha em tempo real");

        // Verificação de CPF para Autocadastro (espelha verify_cpf_users_path)
        app.MapPost("/api/v1/auth/verify-cpf", async (
            VerifyCpfRequest request,
            SolarDbContext db,
            ISigaaAcademicService sigaaService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Cpf))
            {
                return Results.BadRequest(new { error = "CPF é obrigatório." });
            }

            string sanitized = request.Cpf.Replace(".", "").Replace("-", "").Trim();
            var localUser = await db.Users.FirstOrDefaultAsync(u => u.Cpf == sanitized || u.Cpf == request.Cpf);

            if (localUser != null)
            {
                return Results.Ok(new VerifyCpfResponse
                {
                    ExistsInLocal = true,
                    ExistsInSigaa = false,
                    Name = localUser.Name,
                    Email = localUser.Email,
                    Message = "Usuário já cadastrado no Solar LMS. Prossiga para a tela de Login."
                });
            }

            // Consulta no SIGAA externo
            var sigaaRecord = await sigaaService.FindUserByCpfAsync(sanitized);
            if (sigaaRecord != null)
            {
                return Results.Ok(new VerifyCpfResponse
                {
                    ExistsInLocal = false,
                    ExistsInSigaa = true,
                    Name = sigaaRecord.Name,
                    Email = sigaaRecord.Email,
                    Message = "Usuário localizado na base integrada SIGAA. Você pode importar seus dados cadastrais."
                });
            }

            return Results.Ok(new VerifyCpfResponse
            {
                ExistsInLocal = false,
                ExistsInSigaa = false,
                Message = "CPF não localizado na base integrada. É permitido o autocadastro direto."
            });
        })
        .WithName("VerifyCpf")
        .WithSummary("Verifica CPF para autocadastro no Solar LMS ou importação SIGAA");

        // Autocadastro de Usuário (espelha Devise::UsersController#create)
        app.MapPost("/api/v1/auth/register", async (
            RegisterUserRequest request,
            RegisterUserUseCase registerUseCase,
            IJwtTokenService jwtTokenService,
            HttpContext httpContext) =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var enrichedRequest = request with { RemoteIp = clientIp };

            var result = await registerUseCase.ExecuteAsync(enrichedRequest);
            if (!result.Success || result.User == null)
            {
                return Results.BadRequest(result);
            }

            // Gera token JWT amarrado ao dispositivo
            var userSummary = new UserSummaryDto(
                Id: result.User.Id,
                Username: result.User.Username,
                Name: result.User.Name,
                Email: result.User.Email,
                Cpf: result.User.Cpf,
                Role: "student"
            );
            var tokenResponse = jwtTokenService.GenerateToken(userSummary, expirationSeconds: 86400, clientIp: clientIp, userAgent: userAgent);
            var token = tokenResponse.AccessToken;

            // Emite Cookie blindado HttpOnly + Secure + SameSite=Strict
            httpContext.Response.Cookies.Append("solar_access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/"
            });

            return Results.Ok(result with { Token = token });
        })
        .RequireRateLimiting("AuthLimiter")
        .WithName("RegisterUser")
        .WithSummary("Realiza o autocadastro completo de um novo usuário no Solar LMS");

        // Importação e Cadastro via SIGAA
        app.MapPost("/api/v1/auth/import-sigaa", async (
            ImportSigaaUserRequest request,
            RegisterUserUseCase registerUseCase,
            ISigaaAcademicService sigaaService,
            IJwtTokenService jwtTokenService,
            HttpContext httpContext) =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var enrichedRequest = request with { RemoteIp = clientIp };

            var sanitizedCpf = request.Cpf.Replace(".", "").Replace("-", "").Trim();
            var sigaaData = await sigaaService.FindUserByCpfAsync(sanitizedCpf);
            if (sigaaData == null)
            {
                return Results.BadRequest(new RegisterUserResponse
                {
                    Success = false,
                    Message = "Vínculo acadêmico não localizado no SIGAA para este CPF."
                });
            }

            var result = await registerUseCase.ImportFromSigaaAsync(enrichedRequest, sigaaData.Name, sigaaData.Email, sigaaData.EnrollmentCode);
            if (!result.Success || result.User == null)
            {
                return Results.BadRequest(result);
            }

            var userSummary = new UserSummaryDto(
                Id: result.User.Id,
                Username: result.User.Username,
                Name: result.User.Name,
                Email: result.User.Email,
                Cpf: result.User.Cpf,
                Role: result.User.ProfileTypes == 4 ? "teacher" : "student"
            );
            var tokenResponse = jwtTokenService.GenerateToken(userSummary, expirationSeconds: 86400, clientIp: clientIp, userAgent: userAgent);
            var token = tokenResponse.AccessToken;

            httpContext.Response.Cookies.Append("solar_access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/"
            });

            return Results.Ok(result with { Token = token });
        })
        .RequireRateLimiting("AuthLimiter")
        .WithName("ImportSigaaUser")
        .WithSummary("Sincroniza e cria o usuário a partir dos dados institucionais do SIGAA");

        // Solicitação de Recuperação de Senha (Esqueci minha senha - Devise Passwords)
        app.MapPost("/api/v1/auth/forgot-password", async (
            ForgotPasswordRequest request,
            SolarDbContext db,
            PasswordResetService resetService,
            IEmailNotificationService emailService,
            ISigaaAcademicService sigaaService) =>
        {
            var result = await resetService.RequestPasswordResetAsync(request.EmailOrUsernameOrCpf, db, emailService, sigaaService);
            if (!result.Success && !result.IsIntegratedSigaa)
            {
                return Results.BadRequest(result);
            }
            return Results.Ok(result);
        })
        .RequireRateLimiting("AuthLimiter")
        .WithName("ForgotPassword")
        .WithSummary("Envia token/link de redefinição de senha para o e-mail cadastrado");

        // Confirmação de Redefinição de Senha com Token
        app.MapPost("/api/v1/auth/reset-password", async (
            ResetPasswordWithTokenRequest request,
            SolarDbContext db,
            PasswordResetService resetService) =>
        {
            var result = await resetService.ResetPasswordAsync(request.Token, request.NewPassword, request.PasswordConfirmation, db);
            if (!result.Success)
            {
                return Results.BadRequest(result);
            }
            return Results.Ok(result);
        })
        .RequireRateLimiting("AuthLimiter")
        .WithName("ResetPassword")
        .WithSummary("Valida o token e altera a senha do usuário com revogação global de sessões");

        // Provedor OAuth2 de Tokens JWT (Substitui Doorkeeper do Ruby - RFC 6749)
        app.MapPost("/api/v1/oauth/token", async (
            OAuthTokenRequest request,
            SolarDbContext db,
            IPasswordHasher<User> passwordHasher,
            IJwtTokenService jwtService) =>
        {
            if (string.Equals(request.GrantType, "password", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest(new { error = "invalid_request", error_description = "Username e password são obrigatórios." });
                }

                var normalized = request.Username.Trim().ToLowerInvariant();
                var user = await db.Users.FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == normalized ||
                    u.Cpf == request.Username.Trim() ||
                    (u.Email != null && u.Email.ToLower() == normalized)
                );

                if (user == null || string.IsNullOrEmpty(user.EncryptedPassword))
                {
                    return Results.Json(new { error = "invalid_grant", error_description = "Credenciais inválidas." }, statusCode: 400);
                }

                var verification = passwordHasher.VerifyHashedPassword(user, user.EncryptedPassword, request.Password);
                if (verification == PasswordVerificationResult.Failed)
                {
                    return Results.Json(new { error = "invalid_grant", error_description = "Credenciais inválidas." }, statusCode: 400);
                }

                var summary = new UserSummaryDto(
                    Id: user.Id,
                    Username: user.Username,
                    Name: user.Name,
                    Email: user.Email,
                    Cpf: user.Cpf,
                    Role: "Aluno"
                );

                var tokenResponse = jwtService.GenerateToken(summary, 3600);
                return Results.Ok(tokenResponse);
            }
            else if (string.Equals(request.GrantType, "refresh_token", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return Results.BadRequest(new { error = "invalid_request", error_description = "refresh_token é obrigatório." });
                }

                var summary = new UserSummaryDto(1, "aluno1", "Aluno 1 (Demonstração)", "aluno1@solar.ufc.br", "12345678900", "Aluno");
                var refreshed = jwtService.RefreshToken(request.RefreshToken, summary, 3600);
                if (refreshed == null)
                {
                    return Results.Json(new { error = "invalid_grant", error_description = "Refresh token inválido ou expirado." }, statusCode: 400);
                }

                return Results.Ok(refreshed);
            }

            return Results.BadRequest(new { error = "unsupported_grant_type", error_description = "grant_type suportados: 'password' e 'refresh_token'." });
        })
        .WithName("OAuthToken")
        .WithSummary("Emite e renova tokens OAuth2/JWT para aplicativos e integrações");

        return app;
    }
}
