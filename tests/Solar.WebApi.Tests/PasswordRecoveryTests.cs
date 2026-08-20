using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solar.Application.Auth;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class PasswordRecoveryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PasswordRecoveryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task ForgotPassword_NonExistingUser_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(
            EmailOrUsernameOrCpf: "usuario_fantasma_99999"
        ));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<PasswordResetResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("não localizado");
    }

    [Fact]
    public async Task ForgotPassword_IntegratedSigaaUser_Should_Return_SigaaNotice()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
            var integratedUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "alunoteste_sigaa");
            if (integratedUser == null)
            {
                db.Users.Add(new Solar.Domain.Entities.User
                {
                    Username = "alunoteste_sigaa",
                    Name = "Aluno SIGAA",
                    Email = "sigaa@ufc.br",
                    Cpf = "11122233344",
                    Integrated = true,
                    Selfregistration = false,
                    Active = true
                });
                await db.SaveChangesAsync();
            }
        }

        // Usuário integrado SIGAA (selfregistration = false)
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(
            EmailOrUsernameOrCpf: "11122233344"
        ));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PasswordResetResult>();
        result.Should().NotBeNull();
        result!.IsIntegratedSigaa.Should().BeTrue();
        result.Message.Should().Contain("SIGAA");
    }

    [Fact]
    public async Task ForgotPassword_And_ResetPassword_Full_Flow_Should_Succeed()
    {
        var client = _factory.CreateClient();

        // 1. Cria um usuário de autocadastro prévio
        var userCpf = "00055566677";
        var userLogin = "usuario.recuperacao";
        var userEmail = "recuperacao@solar.ufc.br";
        var initialPassword = "SenhaInicial123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest
        {
            Name = "Aluno Recuperacao",
            Cpf = userCpf,
            Username = userLogin,
            Email = userEmail,
            Password = initialPassword,
            PasswordConfirmation = initialPassword
        });

        // 2. Solicita recuperação de senha informando o CPF
        var forgotResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(
            EmailOrUsernameOrCpf: userCpf
        ));

        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var forgotResult = await forgotResponse.Content.ReadFromJsonAsync<PasswordResetResult>();
        forgotResult.Should().NotBeNull();
        forgotResult!.Success.Should().BeTrue();
        forgotResult.GeneratedToken.Should().NotBeNullOrWhiteSpace();

        var token = forgotResult.GeneratedToken!;

        // 3. Redefine a senha utilizando o token gerado
        var newPassword = "NovaSenhaForte2026!";
        var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordWithTokenRequest(
            Token: token,
            NewPassword: newPassword,
            PasswordConfirmation: newPassword
        ));

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetResult = await resetResponse.Content.ReadFromJsonAsync<PasswordResetResult>();
        resetResult.Should().NotBeNull();
        resetResult!.Success.Should().BeTrue();

        // 4. Efetua login com a nova senha redefinida
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Login = userLogin,
            Password = newPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordWithTokenRequest(
            Token: "token_falso_invalido_123",
            NewPassword: "NovaSenha123!",
            PasswordConfirmation: "NovaSenha123!"
        ));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<PasswordResetResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("inválido ou já utilizado");
    }
}
