using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.Domain.Tests.Auth;

public class AuthenticateUserUseCaseTests
{
    private static SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SolarDbContext(options);
    }

    [Fact]
    public async Task Authenticate_By_Username_With_Legacy_Devise_Password_Should_Upgrade_Hash()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var useCase = new AuthenticateUserUseCase(db, hasher);

        string rawPassword = "password123";
        string legacySha1 = DeviseLegacyPasswordHasher<User>.ComputeSha1(rawPassword);

        var user = new User
        {
            Username = "joaosilva",
            Nick = "João",
            Email = "joao@ufc.br",
            Cpf = "11122233344",
            EncryptedPassword = legacySha1,
            Active = true,
            SignInCount = 0
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Login = "joaosilva",
            Password = rawPassword,
            RemoteIp = "192.168.1.50"
        };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.PasswordUpgraded.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("joaosilva");
        result.Token.Should().NotBeNullOrEmpty();

        var updatedUser = await db.Users.FirstAsync(u => u.Username == "joaosilva");
        updatedUser.SignInCount.Should().Be(1);
        updatedUser.CurrentSignInIp.Should().Be("192.168.1.50");
        updatedUser.CurrentSignInAt.Should().NotBeNull();
        updatedUser.EncryptedPassword.Length.Should().BeGreaterThan(40); // Convertido para PBKDF2
    }

    [Fact]
    public async Task Authenticate_By_Cpf_Formatted_Should_Succeed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var useCase = new AuthenticateUserUseCase(db, hasher);

        string rawPassword = "minhasenhaforte";
        var user = new User
        {
            Username = "mariasouza",
            Nick = "Maria",
            Email = "maria@ufc.br",
            Cpf = "55566677788",
            EncryptedPassword = hasher.HashPassword(null!, rawPassword),
            Active = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Login = "555.666.777-88", // CPF formatado
            Password = rawPassword
        };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.User!.Username.Should().Be("mariasouza");
        result.PasswordUpgraded.Should().BeFalse(); // Já estava em PBKDF2
    }

    [Fact]
    public async Task Authenticate_With_Wrong_Password_Should_Fail()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var hasher = new DeviseLegacyPasswordHasher<User>();
        var useCase = new AuthenticateUserUseCase(db, hasher);

        var user = new User
        {
            Username = "prof_carlos",
            Nick = "Carlos",
            Email = "carlos@ufc.br",
            EncryptedPassword = hasher.HashPassword(null!, "correta123"),
            Active = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Login = "prof_carlos",
            Password = "senha_errada"
        };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("inválidos");
    }
}
