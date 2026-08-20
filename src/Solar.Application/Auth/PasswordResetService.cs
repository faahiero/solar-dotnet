using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Application.Integrations.Sigaa;

namespace Solar.Application.Auth;

public record ForgotPasswordRequest(string EmailOrUsernameOrCpf);
public record ResetPasswordWithTokenRequest(string Token, string NewPassword, string? PasswordConfirmation = null);

public record PasswordResetResult(
    bool Success,
    string Message,
    string? GeneratedToken = null,
    bool IsIntegratedSigaa = false,
    bool NeedsRegistrationFirst = false,
    string? SigaaUrl = null,
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
        IEmailNotificationService emailService,
        ISigaaAcademicService? sigaaService = null)
    {
        if (string.IsNullOrWhiteSpace(emailOrUsernameOrCpf))
        {
            return new PasswordResetResult(false, "Informe seu CPF para recuperar sua senha.");
        }

        var rawInput = emailOrUsernameOrCpf.Trim();
        var normalized = rawInput.ToLowerInvariant();
        var sanitizedCpf = rawInput.Replace(".", "").Replace("-", "").Replace(" ", "");

        // 1. Busca primeiro na base de dados local do Solar
        var user = await db.Users.FirstOrDefaultAsync(u =>
            (u.Email != null && u.Email.ToLower() == normalized) ||
            u.Username.ToLower() == normalized ||
            (u.Cpf != null && (u.Cpf == rawInput || u.Cpf.Replace(".", "").Replace("-", "").Replace(" ", "") == sanitizedCpf))
        );

        if (user != null)
        {
            // 2. Se for usuário integrado ao SIGAA
            if (user.Integrated)
            {
                var sigaaLoginUrl = "https://si3.ufc.br/sigaa/verTelaLogin.do";
                if (!user.Selfregistration)
                {
                    return new PasswordResetResult(
                        false,
                        "Identificamos seu vínculo acadêmico no SIGAA. Alunos e professores da UFC devem recuperar ou alterar sua senha diretamente no portal do SIGAA.",
                        IsIntegratedSigaa: true,
                        SigaaUrl: sigaaLoginUrl
                    );
                }

                return new PasswordResetResult(
                    false,
                    "Seus dados foram sincronizados com o SIGAA/SI3. Caso esteja sem acesso, recupere seus dados diretamente no portal do SIGAA.",
                    IsIntegratedSigaa: true,
                    SigaaUrl: sigaaLoginUrl
                );
            }

            // 3. Se for usuário do Solar Cursos / Autocadastro sem e-mail
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new PasswordResetResult(
                    false,
                    "Não há nenhum e-mail cadastrado para o CPF informado. Entre em contato com atendimento@virtual.ufc.br informando seu CPF, nome completo e data de nascimento."
                );
            }

            // 4. Usuário local com e-mail válido: gera o token e dispara o e-mail
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

            lock (_activeTokens)
            {
                _activeTokens[token] = (user.Id, DateTime.UtcNow.AddHours(2));
            }

            await emailService.SendPasswordResetEmailAsync(user.Email, user.Name ?? user.Username, token);

            var maskedEmail = MaskEmail(user.Email);

            return new PasswordResetResult(
                true,
                $"Um e-mail com instruções e código de recuperação de senha foi enviado para {maskedEmail}.",
                GeneratedToken: token,
                MaskedEmail: maskedEmail
            );
        }

        // 5. Se NÃO foi localizado no banco local do Solar
        var isCpf = sanitizedCpf.Length == 11 && sanitizedCpf.All(char.IsDigit);

        if (isCpf && sigaaService != null)
        {
            // Consulta em tempo real na integração do SIGAA
            var sigaaProfile = await sigaaService.FindUserByCpfAsync(sanitizedCpf);
            if (sigaaProfile != null)
            {
                return new PasswordResetResult(
                    false,
                    $"Identificamos seu vínculo acadêmico ativo no SIGAA ({sigaaProfile.Name}). Como este é seu primeiro acesso ao Solar LMS, ative sua conta na aba 'Cadastrar' informando seu CPF ou recupere sua senha no portal do SIGAA.",
                    IsIntegratedSigaa: true,
                    NeedsRegistrationFirst: true,
                    SigaaUrl: "https://si3.ufc.br/sigaa/verTelaLogin.do"
                );
            }

            return new PasswordResetResult(
                false,
                "Nenhum usuário foi encontrado para o CPF informado. Faça seu cadastro na aba 'Cadastrar'."
            );
        }

        if (isCpf)
        {
            return new PasswordResetResult(
                false,
                "Nenhum usuário foi encontrado para o CPF informado. Faça seu cadastro na aba 'Cadastrar'."
            );
        }

        return new PasswordResetResult(
            false,
            "Usuário ou e-mail não localizado no Solar. Se você possui vínculo acadêmico na UFC, informe seu CPF para consultar seu registro no SIGAA."
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
