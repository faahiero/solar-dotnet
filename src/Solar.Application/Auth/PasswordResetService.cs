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
    Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetToken, string? htmlBody = null);
}

public class ConsoleEmailNotificationService : IEmailNotificationService
{
    public Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetToken, string? htmlBody = null)
    {
        Console.WriteLine($"[EMAIL UFC VIRTUAL] Enviando e-mail institucional para {recipientEmail} ({recipientName}) - Token: {resetToken}");
        return Task.CompletedTask;
    }
}

public class UfcHtmlEmailTemplateBuilder
{
    public static string BuildPasswordResetHtml(string recipientName, string resetToken, string resetUrl)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Recuperação de Senha - Solar LMS UFC</title>
          <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333333; }
            .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }
            .header { background-color: #004b8d; padding: 28px; text-align: center; color: #ffffff; }
            .header h1 { margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px; }
            .header p { margin: 4px 0 0 0; font-size: 13px; color: #c9e2ff; }
            .content { padding: 32px 28px; line-height: 1.6; }
            .greeting { font-size: 16px; font-weight: 600; color: #1e293b; margin-bottom: 12px; }
            .btn-container { text-align: center; margin: 30px 0; }
            .btn { display: inline-block; background-color: #0066cc; color: #ffffff !important; text-decoration: none; padding: 14px 32px; font-size: 15px; font-weight: 600; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,102,204,0.3); }
            .token-box { background-color: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 14px; text-align: center; margin: 20px 0; }
            .token-code { font-family: monospace; font-size: 18px; font-weight: 700; color: #004b8d; letter-spacing: 2px; }
            .notice { font-size: 13px; color: #64748b; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }
            .footer { background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }
          </style>
        </head>
        <body>
          <div class="container">
            <div class="header">
              <h1>UNIVERSIDADE FEDERAL DO CEARÁ</h1>
              <p>INSTITUTO UNIVERSIDADE VIRTUAL • SOLAR LMS</p>
            </div>
            <div class="content">
              <div class="greeting">Olá, {{recipientName}}!</div>
              <p>Recebemos uma solicitação para redefinir a sua senha de acesso ao <strong>Solar LMS</strong>.</p>
              <p>Clique no botão abaixo para escolher uma nova senha com segurança:</p>
              <div class="btn-container">
                <a href="{{resetUrl}}" class="btn" target="_blank">Redefinir Minha Senha</a>
              </div>
              <p>Ou, se preferir, utilize o código de autorização diretamente na plataforma:</p>
              <div class="token-box">
                Código de Redefinição: <span class="token-code">{{resetToken}}</span>
              </div>
              <div class="notice">
                <p>⏳ <strong>Atenção:</strong> Este link e código expiram em <strong>2 horas</strong>.</p>
                <p>🛡️ Caso você não tenha solicitado esta redefinição, desconsidere este e-mail. Nenhuma alteração foi realizada em sua conta.</p>
              </div>
            </div>
            <div class="footer">
              Solar LMS • UFC Virtual • Desenvolvido com .NET 10 & React 19<br>
              Campus do Pici - Bloco 901 - Fortaleza/CE
            </div>
          </div>
        </body>
        </html>
        """;
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
        ISigaaAcademicService? sigaaService = null,
        string? appBaseUrl = null)
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

            string baseUrl = appBaseUrl ?? "https://solar.virtual.ufc.br";
            string resetUrl = $"{baseUrl.TrimEnd('/')}/auth/reset-password?token={token}";
            string htmlTemplate = UfcHtmlEmailTemplateBuilder.BuildPasswordResetHtml(user.Name ?? user.Username, token, resetUrl);

            await emailService.SendPasswordResetEmailAsync(user.Email, user.Name ?? user.Username, token, htmlTemplate);

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
            _activeTokens.Remove(token.Trim().ToLowerInvariant()); // Single-use consumption
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return new PasswordResetResult(false, "Usuário não encontrado.");
        }

        user.EncryptedPassword = _passwordHasher.HashPassword(user, newPassword);
        user.PasswordSalt = null;
        user.SessionToken = Guid.NewGuid().ToString("N"); // Invalida todas as sessões anteriores globalmente
        user.FailedAttempts = 0; // Desbloqueia conta se estava bloqueada
        user.LockedAt = null;
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
