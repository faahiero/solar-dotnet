using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Solar.Application.Auth;
using Xunit;

namespace Solar.WebApi.Tests;

public class RegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task VerifyCpf_ExistingLocalUser_Should_Return_ExistsInLocal()
    {
        var client = _factory.CreateClient();

        // 1. Cadastra um usuário para garantir que ele existe
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
        var cpf = $"888{uniqueSuffix}";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest
        {
            Name = "Aluno Existente Teste",
            Cpf = cpf,
            Username = $"aluno_exist_{uniqueSuffix}",
            Password = "SenhaSegura123!",
            PasswordConfirmation = "SenhaSegura123!",
            Email = $"aluno_{uniqueSuffix}@ufc.br"
        });

        // 2. Verifica se o VerifyCpf identifica o usuário existente
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-cpf", new VerifyCpfRequest
        {
            Cpf = cpf
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerifyCpfResponse>();
        result.Should().NotBeNull();
        result!.ExistsInLocal.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyCpf_NewCpf_Should_Return_Allow_Registration()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-cpf", new VerifyCpfRequest
        {
            Cpf = "00099988877"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerifyCpfResponse>();
        result.Should().NotBeNull();
        result!.ExistsInLocal.Should().BeFalse();
        result.ExistsInSigaa.Should().BeFalse();
    }

    [Fact]
    public async Task Register_NewUser_Should_Succeed_And_Return_Cookie_And_Profile()
    {
        var client = _factory.CreateClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var request = new RegisterUserRequest
        {
            Name = "Aluno Novo Teste",
            Cpf = $"777{uniqueSuffix}",
            Username = $"alunonovo_{uniqueSuffix}",
            Password = "SenhaSegura123!",
            PasswordConfirmation = "SenhaSegura123!",
            Email = $"alunonovo_{uniqueSuffix}@ufc.br",
            EmailConfirmation = $"alunonovo_{uniqueSuffix}@ufc.br",
            State = "CE",
            City = "Fortaleza",
            Institution = "Universidade Federal do Ceará"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(request.Username.ToLower());
        result.Token.Should().NotBeNullOrEmpty();

        // Verifica cookie de autenticação
        response.Headers.Should().ContainKey("Set-Cookie");
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.Contains("solar_access_token="));
    }

    [Fact]
    public async Task Register_PasswordMismatch_Should_Fail()
    {
        var client = _factory.CreateClient();

        var request = new RegisterUserRequest
        {
            Name = "Aluno Teste Mismatch",
            Cpf = "55544433322",
            Username = "aluno_mismatch",
            Password = "Senha123",
            PasswordConfirmation = "OutraSenha",
            Email = "mismatch@ufc.br"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("confirmação de senha");
    }
}
