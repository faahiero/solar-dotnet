using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Application.Common;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Endpoints;

namespace Solar.WebApi.Tests;

public class AdvancedAuthSecurityTests
{
    private SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(databaseName: $"SolarAuthSecurityTestDb_{Guid.NewGuid()}")
            .Options;
        return new SolarDbContext(options);
    }

    [Fact]
    public void PasswordPolicy_ShouldRejectWeakAndAcceptStrongPasswords()
    {
        // Arrange
        var policy = new PasswordPolicyService();

        // Act & Assert
        var weak1 = policy.ValidatePassword("123456");
        Assert.False(weak1.IsValid);

        var weak2 = policy.ValidatePassword("solar123");
        Assert.False(weak2.IsValid);

        var weak3 = policy.ValidatePassword("alunoteste2026", username: "alunoteste");
        Assert.False(weak3.IsValid);

        var strong = policy.ValidatePassword("S0l@rUfc#2026!#Adv");
        Assert.True(strong.IsValid);
        Assert.Equal("Muito Forte", strong.Strength);
        Assert.Empty(strong.Errors);
    }

    [Fact]
    public async Task AuthenticateUserUseCase_ShouldLockAccountAfter5FailedAttempts()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var user = new User
        {
            Username = "usuario_lockout_teste",
            EncryptedPassword = hasher.HashPassword(null!, "Correta#2026!"),
            Active = true,
            Registered = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var useCase = new AuthenticateUserUseCase(db, hasher);

        // Act: 4 falhas
        for (int i = 0; i < 4; i++)
        {
            var res = await useCase.ExecuteAsync(new LoginRequest
            {
                Login = "usuario_lockout_teste",
                Password = "SenhaErrada"
            });
            Assert.False(res.Success);
            Assert.Contains("inválidos", res.Message);
        }

        // 5ª falha -> aciona lockout
        var fifthResult = await useCase.ExecuteAsync(new LoginRequest
        {
            Login = "usuario_lockout_teste",
            Password = "SenhaErrada"
        });
        Assert.False(fifthResult.Success);
        Assert.Contains("bloqueada temporariamente", fifthResult.Message);

        // Tentativa subsequente mesmo com senha correta deve permanecer bloqueada
        var lockedResult = await useCase.ExecuteAsync(new LoginRequest
        {
            Login = "usuario_lockout_teste",
            Password = "Correta#2026!"
        });
        Assert.False(lockedResult.Success);
        Assert.Contains("bloqueada temporariamente", lockedResult.Message);
    }

    [Fact]
    public async Task PasswordResetService_ShouldInvalidateAllSessionsGloballyOnPasswordReset()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var originalSessionToken = "sessao_antiga_12345";
        var user = new User
        {
            Username = "aluno_reset_sessao",
            Email = "aluno.reset@ufc.br",
            EncryptedPassword = hasher.HashPassword(null!, "SenhaAntiga#123"),
            SessionToken = originalSessionToken,
            FailedAttempts = 5,
            LockedAt = DateTime.UtcNow,
            Active = true,
            Registered = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var emailService = new ConsoleEmailNotificationService();
        var resetService = new PasswordResetService(hasher);

        // 1. Solicita redefinição
        var requestResult = await resetService.RequestPasswordResetAsync("aluno.reset@ufc.br", db, emailService);
        Assert.True(requestResult.Success);
        Assert.NotNull(requestResult.GeneratedToken);

        // 2. Executa redefinição de senha
        var resetResult = await resetService.ResetPasswordAsync(requestResult.GeneratedToken!, "NovaSenha#2026!Forte", db);
        Assert.True(resetResult.Success);

        // 3. Verifica que a sessão anterior foi invalidada, tentativas zeradas e conta desbloqueada
        var updatedUser = await db.Users.FirstAsync(u => u.Username == "aluno_reset_sessao");
        Assert.NotEqual(originalSessionToken, updatedUser.SessionToken);
        Assert.Equal(0, updatedUser.FailedAttempts);
        Assert.Null(updatedUser.LockedAt);

        // 4. Token deve ser de uso único (não pode ser reutilizado)
        var secondResetResult = await resetService.ResetPasswordAsync(requestResult.GeneratedToken!, "OutraSenha#2026!", db);
        Assert.False(secondResetResult.Success);
        Assert.Contains("já utilizado", secondResetResult.Message);
    }

    [Fact]
    public async Task RegisterUserUseCase_ShouldRecordLgpdConsentProperly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var policy = new PasswordPolicyService();
        var useCase = new RegisterUserUseCase(db, hasher, policy);

        var request = new RegisterUserRequest
        {
            Name = "Aluno LGPD Teste",
            Cpf = "99887766554",
            Username = "alunolgpd",
            Email = "aluno.lgpd@ufc.br",
            Password = "SenhaForte#2026!",
            PasswordConfirmation = "SenhaForte#2026!",
            AcceptTerms = true,
            TermsVersion = "v2.0_2026",
            RemoteIp = "189.100.20.10"
        };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        var createdUser = await db.Users.FirstAsync(u => u.Username == "alunolgpd");
        Assert.NotNull(createdUser.TermsAcceptedAt);
        Assert.Equal("189.100.20.10", createdUser.TermsAcceptedIp);
        Assert.Equal("v2.0_2026", createdUser.TermsVersion);
    }
}
