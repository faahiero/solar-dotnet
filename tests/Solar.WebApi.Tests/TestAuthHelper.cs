using System.Net.Http.Headers;
using Solar.Application.Auth;
using Solar.Infrastructure.Identity;

namespace Solar.WebApi.Tests;

public static class TestAuthHelper
{
    public static HttpClient AsAdmin(this HttpClient client, long userId = 1, string username = "admin")
    {
        var tokenService = new JwtTokenService();
        var user = new UserSummaryDto(userId, username, "Administrador Teste", "admin@solar.ufc.br", "12345678900", "admin");
        var token = tokenService.GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    public static HttpClient AsTeacher(this HttpClient client, long userId = 2, string username = "prof.fabricio")
    {
        var tokenService = new JwtTokenService();
        var user = new UserSummaryDto(userId, username, "Prof. Fabrício Silva", "prof@solar.ufc.br", "99988877766", "teacher");
        var token = tokenService.GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    public static HttpClient AsStudent(this HttpClient client, long userId = 3, string username = "aluno1")
    {
        var tokenService = new JwtTokenService();
        var user = new UserSummaryDto(userId, username, "Aluno Teste", "aluno@solar.ufc.br", "12345678901", "student");
        var token = tokenService.GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }
}
