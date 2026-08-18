using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Solar.Infrastructure.Identity;

/// <summary>
/// Hasher híbrido para ASP.NET Identity com suporte a migração transparente
/// dos hashes legados do Devise em Ruby on Rails (SHA1 e SHA1(MD5)).
/// Quando um usuário autentica com sucesso via hash legado, o Identity
/// automaticamente atualiza o hash no banco para o padrão moderno (PBKDF2/HMAC-SHA512).
/// </summary>
public class DeviseLegacyPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private readonly PasswordHasher<TUser> _defaultHasher = new();

    public string HashPassword(TUser user, string password)
    {
        return _defaultHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        // 1. Tentar verificar com o hasher moderno do ASP.NET Identity (v2 ou v3)
        var defaultResult = _defaultHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        if (defaultResult != PasswordVerificationResult.Failed)
        {
            return defaultResult;
        }

        // 2. Se falhar, verificar se o hash armazenado é um hash legado Devise (SHA-1 possui 40 caracteres hexadecimais)
        if (hashedPassword.Length == 40 && IsHexString(hashedPassword))
        {
            // Formato A: SHA1 puro (Devise padrão do Solar)
            string sha1Provided = ComputeSha1(providedPassword);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(sha1Provided),
                    Encoding.UTF8.GetBytes(hashedPassword.ToLowerInvariant())))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Formato B: SHA1(MD5(senha)) (Usado em usuários integrados / autoinstrucionais no Solar)
            string md5Provided = ComputeMd5(providedPassword);
            string sha1Md5Provided = ComputeSha1(md5Provided);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(sha1Md5Provided),
                    Encoding.UTF8.GetBytes(hashedPassword.ToLowerInvariant())))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }
        }

        return PasswordVerificationResult.Failed;
    }

    public static string ComputeSha1(string input)
    {
        byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string ComputeMd5(string input)
    {
        byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool IsHexString(string value)
    {
        return value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}
