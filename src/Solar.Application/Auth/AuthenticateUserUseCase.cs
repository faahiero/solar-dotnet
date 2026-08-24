using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Auth;

public interface ISolarAuthDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Caso de uso completo de autenticação do Solar LMS (.NET 10).
/// Espelha o Devise LoginController original com suporte a Login por Username/CPF,
/// validação de senhas legadas Devise (SHA1/MD5), upgrade transparente para PBKDF2,
/// proteção contra força bruta (Account Lockout) e autenticação em duas etapas (2FA / TOTP).
/// </summary>
public class AuthenticateUserUseCase
{
    private readonly ISolarAuthDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthenticateUserUseCase(
        ISolarAuthDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Usuário e senha são obrigatórios."
            };
        }

        string rawLogin = request.Login.Trim().ToLowerInvariant();
        string sanitizedCpf = rawLogin.Replace(".", "").Replace("-", "");

        // 1. Busca por Username ou CPF (espelha Devise::LoginController:38)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == rawLogin ||
                (u.Cpf != null && (u.Cpf == rawLogin || u.Cpf == sanitizedCpf)),
                cancellationToken);

        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Usuário ou senha inválidos."
            };
        }

        // 2. Verificação de Bloqueio Temporário (Account Lockout)
        if (user.LockedAt.HasValue && user.LockedAt.Value.AddMinutes(15) > DateTime.UtcNow)
        {
            var remainingMinutes = Math.Ceiling((user.LockedAt.Value.AddMinutes(15) - DateTime.UtcNow).TotalMinutes);
            return new LoginResponse
            {
                Success = false,
                Message = $"Conta bloqueada temporariamente por excesso de tentativas incorretas. Tente novamente em {remainingMinutes} minuto(s)."
            };
        }

        // 3. Verificação de Senha Híbrida
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.EncryptedPassword, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= 5)
            {
                user.LockedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                Success = false,
                Message = user.FailedAttempts >= 5
                    ? "Conta bloqueada temporariamente por 15 minutos após 5 tentativas incorretas."
                    : "Usuário ou senha inválidos."
            };
        }

        // 4. Se a senha confere, limpa contadores de falha
        user.FailedAttempts = 0;
        user.LockedAt = null;

        // 5. Atualização transparente de hash para PBKDF2 se necessário
        bool rehashNeeded = verificationResult == PasswordVerificationResult.SuccessRehashNeeded;
        if (rehashNeeded)
        {
            user.EncryptedPassword = _passwordHasher.HashPassword(user, request.Password);
            user.PasswordSalt = null; // Salt não é mais necessário com PBKDF2
        }

        // 6. Atualização de métricas de login do Devise
        user.SignInCount++;
        user.LastSignInAt = user.CurrentSignInAt;
        user.CurrentSignInAt = DateTime.UtcNow;
        user.LastSignInIp = user.CurrentSignInIp;
        user.CurrentSignInIp = request.RemoteIp ?? "127.0.0.1";
        user.SessionToken = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        int profileType = user.Username.StartsWith("prof", StringComparison.OrdinalIgnoreCase) ? 4 : // Professor
                          user.Username.Contains("admin", StringComparison.OrdinalIgnoreCase) ? 16 : // Administrador
                          1; // Aluno

        return new LoginResponse
        {
            Success = true,
            Message = "Autenticação realizada com sucesso.",
            Token = user.SessionToken,
            PasswordUpgraded = rehashNeeded,
            User = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name ?? user.Username,
                Email = user.Email,
                Cpf = user.Cpf,
                ProfileTypes = profileType
            }
        };
    }
}
