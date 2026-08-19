using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Auth;

public record ForgotPasswordRequest(string EmailOrUsername);
public record ResetPasswordWithTokenRequest(string Token, string NewPassword);

public record PasswordResetResult(bool Success, string Message, string? GeneratedToken = null);

public interface IEmailNotificationService
{
    Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetToken);
}

public class ConsoleEmailNotificationService : IEmailNotificationService
{
    public Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetToken)
    {
        Console.WriteLine($"[EMAIL SERVICE] Enviado e-mail de recuperação de senha para: {recipientEmail} ({recipientName}) - Token: {resetToken}");
        return Task.CompletedTask;
    }
}

public class PasswordResetService
{
    private static readonly Dictionary<string, (long UserId, DateTime ExpiresAt)> _activeTokens = new();
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordResetService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task<PasswordResetResult> RequestPasswordResetAsync(
        string emailOrUsername,
        ISolarAuthDbContext db,
        IEmailNotificationService emailService)
    {
        if (string.IsNullOrWhiteSpace(emailOrUsername))
        {
            return new PasswordResetResult(false, "E-mail ou nome de usuário é obrigatório.");
        }

        var normalized = emailOrUsername.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u =>
            (u.Email != null && u.Email.ToLower() == normalized) ||
            u.Username.ToLower() == normalized ||
            u.Cpf == emailOrUsername.Trim()
        );

        if (user == null)
        {
            return new PasswordResetResult(true, "Se os dados informados coincidirem com uma conta cadastrada, as instruções de recuperação foram enviadas.");
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        lock (_activeTokens)
        {
            _activeTokens[token] = (user.Id, DateTime.UtcNow.AddHours(2));
        }

        await emailService.SendPasswordResetEmailAsync(user.Email ?? user.Username + "@solar.ufc.br", user.Name ?? user.Username, token);

        return new PasswordResetResult(true, "Instruções de redefinição de senha enviadas com sucesso.", token);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        string token,
        string newPassword,
        ISolarAuthDbContext db)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new PasswordResetResult(false, "Token de recuperação inválido.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return new PasswordResetResult(false, "A nova senha deve possuir no mínimo 6 caracteres.");
        }

        long userId = 0;
        lock (_activeTokens)
        {
            if (!_activeTokens.TryGetValue(token, out var tokenData))
            {
                return new PasswordResetResult(false, "Token de recuperação não encontrado ou já utilizado.");
            }

            if (DateTime.UtcNow > tokenData.ExpiresAt)
            {
                _activeTokens.Remove(token);
                return new PasswordResetResult(false, "Token de recuperação expirado. Solicite um novo link.");
            }

            userId = tokenData.UserId;
            _activeTokens.Remove(token);
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return new PasswordResetResult(false, "Usuário não encontrado.");
        }

        user.EncryptedPassword = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new PasswordResetResult(true, "Senha redefinida com sucesso! Você já pode realizar o login com suas novas credenciais.");
    }
}
