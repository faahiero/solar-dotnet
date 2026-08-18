using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Solar.Infrastructure.Identity;
using Xunit;

namespace Solar.Domain.Tests.Identity;

public class DeviseLegacyPasswordHasherTests
{
    private class DummyUser { public string Id { get; set; } = "1"; }
    private readonly DeviseLegacyPasswordHasher<DummyUser> _hasher = new();
    private readonly DummyUser _user = new();

    [Fact]
    public void Should_Authenticate_And_Signal_Rehash_For_Legacy_Sha1_Password()
    {
        // Arrange
        // Em Ruby: Digest::SHA1.hexdigest("solar123") = "f05786f1f45dc2fc8036573c734898144b6c41b8" (exemplo gerado)
        string plainPassword = "minhasenhasolar";
        string legacySha1Hash = DeviseLegacyPasswordHasher<DummyUser>.ComputeSha1(plainPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(_user, legacySha1Hash, plainPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void Should_Authenticate_And_Signal_Rehash_For_Legacy_Sha1_Md5_Password()
    {
        // Arrange (Usuário integrado: SHA1(MD5("senha123")))
        string plainPassword = "alunointegrado";
        string md5 = DeviseLegacyPasswordHasher<DummyUser>.ComputeMd5(plainPassword);
        string legacySha1Md5Hash = DeviseLegacyPasswordHasher<DummyUser>.ComputeSha1(md5);

        // Act
        var result = _hasher.VerifyHashedPassword(_user, legacySha1Md5Hash, plainPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void Should_Fail_Authentication_When_Legacy_Password_Is_Incorrect()
    {
        // Arrange
        string correctPassword = "senhacorreta";
        string legacyHash = DeviseLegacyPasswordHasher<DummyUser>.ComputeSha1(correctPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(_user, legacyHash, "senhaerrada");

        // Assert
        result.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Should_Authenticate_Modern_AspNet_Identity_Password_Without_Rehash_Needed()
    {
        // Arrange
        string plainPassword = "NovaSenhaForte2026!";
        string modernHash = _hasher.HashPassword(_user, plainPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(_user, modernHash, plainPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.Success);
    }
}
