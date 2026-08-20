using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Auth;

public record ForgotPasswordRequest(string EmailOrUsernameOrCpf);
public record ResetPasswordWithTokenRequest(string Token, string NewPassword, string? PasswordConfirmation = null);

public record PasswordResetResult(
    bool Success,
    string Message,
    string? GeneratedToken = null,
    bool IsIntegratedSigaa = false,
    string? MaskedEmail = null
);

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
        string emailOrUsernameOrCpf,
        ISolarAuthDbContext db,
        IEmailNotificationService emailService)
    {
        if (string.IsNullOrWhiteSpace(emailOrUsernameOrCpf))
        {
            return new PasswordResetResult(false, "Informe seu CPF, e-mail ou nome de usuário.");
        }

        var rawInput = emailOrUsernameOrCpf.Trim();
        var normalized = rawInput.ToLowerInvariant();
        var sanitizedCpf = rawInput.Replace(".", "").Replace("-", "");

        var user = await db.Users.FirstOrDefaultAsync(u =>
            (u.Email != null && u.Email.ToLower() == normalized) ||
            u.Username.ToLower() == normalized ||
            (u.Cpf != null && (u.Cpf == rawInput || u.Cpf.Replace(".", "").Replace("-", "") == sanitizedCpf))
        );

        if (user == null)
        {
            return new PasswordResetResult(false, "Usuário ou CPF não localizado no Solar LMS.");
        }

        // Se for usuário integrado ao SIGAA sem autocadastro próprio
        if (user.Integrated && !user.Selfregistration)
        {
            return new PasswordResetResult(
                false,
                "Usuários com vínculo acadêmico integrado devem alterar ou recuperar sua senha diretamente no portal do SIGAA.",
                IsIntegratedSigaa: true
            );
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return new PasswordResetResult(false, "Não há e-mail cadastrado para esta conta. Procure a coordenação do seu curso.");
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        lock (_activeTokens)
        {
            _activeTokens[token] = (user.Id, DateTime.UtcNow.AddHours(2));
        }

        await emailService.SendPasswordResetEmailAsync(user.Email, user.Name ?? user.Username, token);

        // Mascara o e-mail para exibição segura (ex: j***@ufc.br)
        var maskedEmail = MaskEmail(user.Email);

        return new PasswordResetResult(
            true,
            $"Instruções e código de recuperação foram enviados para o e-mail {maskedEmail}.",
            GeneratedToken: token,
            MaskedEmail: maskedEmail
        );
    }

    public Task<PasswordResetResult> ResetPasswordAsync(
        string token,
        string newPassword,
        ISolarAuthDbContext db)
    {
        return ResetPasswordAsync(token, newPassword, null, db);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        string token,
        string newPassword,
        string? passwordConfirmation,
        ISolarAuthDbContext db)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new PasswordResetResult(false, "Código/Token de recuperação é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return new PasswordResetResult(false, "A nova senha deve possuir no mínimo 6 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(passwordConfirmation) && newPassword != passwordConfirmation)
        {
            return new PasswordResetResult(false, "A confirmação de senha não confere.");
        }

        long userId = 0;
        lock (_activeTokens)
        {
            if (!_activeTokens.TryGetValue(token.Trim().ToLowerInvariant(), out var tokenData))
            {
                return new PasswordResetResult(false, "Código de recuperação inválido ou já utilizado.");
            }

            if (DateTime.UtcNow > tokenData.ExpiresAt)
            {
                _activeTokens.Remove(token.Trim().ToLowerInvariant());
                return new PasswordResetResult(false, "Código de recuperação expirado. Solicite uma nova redefinição.");
            }

            userId = tokenData.UserId;
            _activeTokens.Remove(token.Trim().ToLowerInvariant());
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return new PasswordResetResult(false, "Usuário não encontrado.");
        }

        user.EncryptedPassword = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new PasswordResetResult(true, "Sua senha foi redefinida com sucesso! Você já pode realizar o login.");
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return email;
        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];
        if (name.Length <= 2) return $"{name[0]}*@{domain}";
        return $"{name[0]}{new string('*', name.Length - 2)}{name[^1]}@{domain}";
    }
}
