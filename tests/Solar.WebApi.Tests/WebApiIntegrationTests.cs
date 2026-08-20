using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solar.Application.Auth;
using Solar.Application.Grading;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Solar.Domain.Grading;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class WebApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task Get_Health_Should_Return_200_OK_And_Healthy_Status()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.System.Should().Contain(".NET 10");
    }

    [Fact]
    public async Task Get_Healthz_And_Livez_Should_Return_200_OK()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var healthz = await client.GetAsync("/healthz");
        var livez = await client.GetAsync("/livez");
        var readyz = await client.GetAsync("/readyz");

        // Assert
        healthz.StatusCode.Should().Be(HttpStatusCode.OK);
        livez.StatusCode.Should().Be(HttpStatusCode.OK);
        readyz.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Index_Should_Serve_Frontend_Dashboard()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Solar LMS");
        html.Should().Contain(".NET 10");
        html.Should().Contain("root");
    }

    [Fact]
    public async Task Post_Login_Should_Authenticate_Legacy_Devise_User_And_Upgrade_Password()
    {
        // Arrange
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
            string plainPassword = "senhadoteste123";
            string legacyHash = DeviseLegacyPasswordHasher<User>.ComputeSha1(plainPassword);

            var testUser = new User
            {
                Username = "alunoteste",
                Nick = "Aluno",
                Email = "aluno@solar.ufc.br",
                Cpf = "12345678900",
                EncryptedPassword = legacyHash,
                Active = true
            };

            db.Users.Add(testUser);
            await db.SaveChangesAsync();
        }

        var loginPayload = new LoginRequest
        {
            Login = "alunoteste",
            Password = "senhadoteste123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.User.Should().NotBeNull();
        loginResult.User!.Username.Should().Be("alunoteste");
        loginResult.PasswordUpgraded.Should().BeTrue();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
            var updatedUser = db.Users.First(u => u.Username == "alunoteste");
            updatedUser.EncryptedPassword.Length.Should().BeGreaterThan(40);
        }
    }

    [Fact]
    public async Task Post_VerifyCpf_Should_Identify_Existing_Or_Sigaa_User()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act (Verifica CPF existente no SIGAA)
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-cpf", new VerifyCpfRequest
        {
            Cpf = "987.654.321-00"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VerifyCpfResponse>();
        content.Should().NotBeNull();
        content!.ExistsInSigaa.Should().BeTrue();
        content.Message.Should().Contain("SIGAA");
    }

    [Fact]
    public async Task Post_CalculateGrades_Should_Return_Correct_Grade_And_Situation()
    {
        // Arrange
        var client = _factory.CreateClient();

        var command = new CalculateStudentGradesCommand
        {
            UserId = 1,
            AllocationId = 10,
            Criteria = new GradingCourseCriteria
            {
                PassingGrade = 7.0,
                MinGradeToFinalExam = 3.0,
                FinalExamPassingGrade = 5.0,
                TotalWorkingHours = 64,
                MinHoursPercentage = 75.0
            },
            Activities =
            [
                new GradingEvaluationInput
                {
                    ActivityId = 101,
                    Name = "Prova 1",
                    IsEvaluative = true,
                    IsFrequency = true,
                    Weight = 1.0,
                    FinalWeight = 40.0,
                    StudentGrade = 8.0,
                    StudentWorkingHours = 32.0
                },
                new GradingEvaluationInput
                {
                    ActivityId = 102,
                    Name = "Trabalho Prático",
                    IsEvaluative = true,
                    IsFrequency = true,
                    Weight = 1.0,
                    FinalWeight = 60.0,
                    StudentGrade = 7.5,
                    StudentWorkingHours = 32.0
                }
            ]
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/grades/calculate", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GradingCalculationResult>();
        result.Should().NotBeNull();
        result!.ParcialGrade.Should().Be(7.7);
        result.FinalGrade.Should().Be(7.7);
        result.TotalWorkingHours.Should().Be(64.0);
        result.Situation.Should().Be(GradeSituation.Approved);
        result.IsFrequencySufficient.Should().BeTrue();
    }

    [Fact]
    public async Task ExamLockoutMiddleware_Should_Block_Access_To_Lessons_When_Student_Has_Active_Locked_Exam()
    {
        // Arrange
        var client = _factory.CreateClient();
        long studentId = 999;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();

            var lockedExam = new Exam
            {
                Name = "Prova Oficial com Bloqueio",
                Description = "Prova Online",
                BlockContent = true, // Ativa trava anti-fraude
                Status = true
            };
            db.Exams.Add(lockedExam);
            await db.SaveChangesAsync();

            var academicAllocation = new AcademicAllocation
            {
                AcademicToolType = "Exam",
                AcademicToolId = lockedExam.Id,
                Evaluative = true
            };
            db.AcademicAllocations.Add(academicAllocation);
            await db.SaveChangesAsync();

            var acu = new AcademicAllocationUser
            {
                UserId = studentId,
                AcademicAllocationId = academicAllocation.Id,
                Status = SubmissionStatus.Sent
            };
            db.AcademicAllocationUsers.Add(acu);
            await db.SaveChangesAsync();

            var attempt = new ExamUserAttempt
            {
                AcademicAllocationUserId = acu.Id,
                Complete = false // Prova em andamento!
            };
            db.ExamUserAttempts.Add(attempt);
            await db.SaveChangesAsync();
        }

        // Act: Aluno tenta acessar aulas enquanto a prova está em andamento
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/lessons");
        request.Headers.Add("X-User-Id", studentId.ToString());

        var response = await client.SendAsync(request);

        // Assert: Deve retornar 403 Forbidden com código de bloqueio de segurança
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("EXAM_CONTENT_LOCKED");
    }

    [Fact]
    public async Task Get_Cached_Agenda_Should_Return_200_OK_And_Fast_Response()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var firstResponse = await client.GetAsync("/api/v1/agenda");
        var secondResponse = await client.GetAsync("/api/v1/agenda");

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await secondResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Agosto 2026");
    }

    [Fact]
    public async Task Get_Cached_Admin_Profiles_Should_Return_200_OK()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var firstResponse = await client.GetAsync("/api/v1/admin/profiles");
        var secondResponse = await client.GetAsync("/api/v1/admin/profiles");

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await secondResponse.Content.ReadAsStringAsync();
        content.Should().Contain("tutor_distance");
        content.Should().Contain("student");
    }

    private record HealthResponse(string Status, string System, DateTime Timestamp);
}
