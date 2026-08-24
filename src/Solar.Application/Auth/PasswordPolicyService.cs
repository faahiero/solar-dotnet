using System.Text.RegularExpressions;

namespace Solar.Application.Auth;

public record PasswordValidationResult(
    bool IsValid,
    int Score, // 0 a 100
    string Strength, // "Fraca", "Média", "Forte", "Muito Forte"
    List<string> Errors
);

public class PasswordPolicyService
{
    private static readonly HashSet<string> CommonWeakPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456", "12345678", "123456789", "senha123", "solar123", "admin123", "password", "qwerty",
        "mudar123", "ufcvirtual", "solar2026", "brasil123", "secret123", "111111", "000000"
    };

    public PasswordValidationResult ValidatePassword(string password, string? username = null, string? cpf = null)
    {
        var errors = new List<string>();
        int score = 0;

        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordValidationResult(false, 0, "Inválida", new List<string> { "A senha não pode estar em branco." });
        }

        // 1. Tamanho
        if (password.Length < 8)
        {
            errors.Add("A senha deve conter no mínimo 8 caracteres (recomendação de segurança NIST).");
        }
        else
        {
            score += Math.Min(30, password.Length * 3);
        }

        // 2. Lista de senhas fracas comuns
        if (CommonWeakPasswords.Contains(password.Trim()))
        {
            errors.Add("A senha escolhida é muito comum e fácil de ser descoberta. Escolha outra mais segura.");
            return new PasswordValidationResult(false, 10, "Fraca", errors);
        }

        // 3. Checagem contra dados pessoais óbvios
        if (!string.IsNullOrWhiteSpace(username) && password.Contains(username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("A senha não deve conter o seu nome de usuário.");
        }

        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var cleanCpf = Regex.Replace(cpf, @"\D", "");
            if (cleanCpf.Length >= 6 && password.Contains(cleanCpf[..6]))
            {
                errors.Add("A senha não deve conter trechos do seu CPF.");
            }
        }

        // 4. Variedade de Caracteres
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        if (hasUpper) score += 15;
        if (hasLower) score += 15;
        if (hasDigit) score += 20;
        if (hasSpecial) score += 20;

        string strength = score switch
        {
            >= 80 => "Muito Forte",
            >= 60 => "Forte",
            >= 40 => "Média",
            _ => "Fraca"
        };

        bool isValid = errors.Count == 0 && score >= 40;

        return new PasswordValidationResult(isValid, Math.Min(100, score), strength, errors);
    }
}
