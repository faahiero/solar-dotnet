using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class PasswordResetTests
{
    private SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SolarDbContext(options);
    }

    [Fact]
    public async Task RequestPasswordReset_And_ResetPassword_ShouldSuccessfullyChangeUserPassword()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = new User
        {
            Id = 10,
            Username = "alunoredefinir",
            Email = "redefinir@solar.ufc.br",
            EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("senhaantiga"),
            Active = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new PasswordResetService(new DeviseLegacyPasswordHasher<User>());
        var emailService = new ConsoleEmailNotificationService();

        // Act 1: Solicita redefinição
        var requestResult = await service.RequestPasswordResetAsync("redefinir@solar.ufc.br", db, emailService);

        Assert.True(requestResult.Success);
        Assert.NotNull(requestResult.GeneratedToken);

        // Act 2: Redefine com o token gerado
        var resetResult = await service.ResetPasswordAsync(requestResult.GeneratedToken, "senhanovanova123", db);

        // Assert
        Assert.True(resetResult.Success);

        var updatedUser = await db.Users.FindAsync((long)10);
        Assert.NotNull(updatedUser);
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var verifyResult = hasher.VerifyHashedPassword(updatedUser, updatedUser.EncryptedPassword!, "senhanovanova123");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new PasswordResetService(new DeviseLegacyPasswordHasher<User>());

        // Act
        var result = await service.ResetPasswordAsync("token_falso_inexistente", "novaSenha123", db);

        // Assert
        Assert.False(result.Success);
    }
}
