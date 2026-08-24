using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Endpoints;
using Xunit;

namespace Solar.WebApi.Tests;

public class AdminCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminCrudTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task Groups_Crud_Flow_Should_Create_Get_Update_And_Delete_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        long offerId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
            var offer = new Offer
            {
                SemesterId = 1,
                CourseId = 999,
                CurriculumUnitId = 999,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Offers.Add(offer);
            await db.SaveChangesAsync();
            offerId = offer.Id;
        }

        // 1. Create Group (POST)
        var createReq = new CreateGroupRequest(
            OfferId: offerId,
            Code: $"TURMA-{Guid.NewGuid().ToString("N")[..6]}",
            Name: "Turma de Teste 01",
            Location: "Polo Fortaleza",
            Status: true,
            Integrated: false,
            MainGroupId: null,
            DigitalClassDirectoryId: null
        );

        var createRes = await client.PostAsJsonAsync("/api/v1/groups", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Get Groups (GET)
        var listRes = await client.GetAsync($"/api/v1/groups?offerId={offerId}");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Get Group by ID (GET)
        var groupJson = await createRes.Content.ReadFromJsonAsync<GroupTestDto>();
        groupJson.Should().NotBeNull();
        long groupId = groupJson!.Id;

        var getRes = await client.GetAsync($"/api/v1/groups/{groupId}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Update Group (PUT)
        var updateReq = new UpdateGroupRequest(
            OfferId: null,
            Code: $"TURMA-ALT-{Guid.NewGuid().ToString("N")[..6]}",
            Name: "Turma de Teste Alterada",
            Location: "Polo Caucaia",
            Status: false,
            Integrated: true,
            MainGroupId: null,
            DigitalClassDirectoryId: null
        );

        var putRes = await client.PutAsJsonAsync($"/api/v1/groups/{groupId}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Delete Group (DELETE)
        var delRes = await client.DeleteAsync($"/api/v1/groups/{groupId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Allocations_Crud_Flow_Should_Create_Get_Update_And_Delete_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        long userId;
        int profileId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();

            var user = new User
            {
                Username = $"user_alloc_{Guid.NewGuid().ToString("N")[..6]}",
                Name = "Usuario Teste Alocacao",
                Email = "alloc@test.com",
                Cpf = "77788899900",
                Active = true
            };
            db.Users.Add(user);

            var profile = new Profile
            {
                Name = "Professor Titular",
                Types = ProfileType.ClassResponsible,
                Description = "Docente"
            };
            db.Profiles.Add(profile);

            await db.SaveChangesAsync();

            userId = user.Id;
            profileId = (int)profile.Id;
        }

        // 1. Create Allocation (POST)
        var createReq = new CreateAllocationRequest(
            UserId: userId,
            ProfileId: profileId,
            AllocationTagId: 100,
            Status: AllocationStatus.Activated
        );

        var createRes = await client.PostAsJsonAsync("/api/v1/allocations", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var allocJson = await createRes.Content.ReadFromJsonAsync<AllocationTestDto>();
        allocJson.Should().NotBeNull();
        long allocId = allocJson!.Id;

        // 2. Get Allocations (GET)
        var listRes = await client.GetAsync($"/api/v1/allocations?userId={userId}");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update Allocation (PUT)
        var updateReq = new UpdateAllocationRequest(
            AllocationTagId: 100,
            ProfileId: profileId,
            Status: AllocationStatus.Activated,
            ParcialGrade: 8.5,
            FinalExamGrade: null,
            FinalGrade: 8.5,
            WorkingHours: 64,
            GradeSituation: GradeSituation.Approved,
            UpdatedByUserId: 1,
            OriginGroupId: null
        );

        var putRes = await client.PutAsJsonAsync($"/api/v1/allocations/{allocId}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Delete Allocation (DELETE)
        var delRes = await client.DeleteAsync($"/api/v1/allocations/{allocId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Semesters_Crud_Flow_Should_Create_Get_Update_And_Delete_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        string semesterName = $"2026.{Guid.NewGuid().ToString("N")[..4]}";

        // 1. Create Semester (POST)
        var createReq = new CreateSemesterRequest(
            Name: semesterName,
            OfferScheduleId: 1,
            EnrollmentScheduleId: 1
        );

        var createRes = await client.PostAsJsonAsync("/api/v1/semesters", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var semJson = await createRes.Content.ReadFromJsonAsync<SemesterTestDto>();
        semJson.Should().NotBeNull();
        long semesterId = semJson!.Id;

        // 2. Get Semesters (GET)
        var listRes = await client.GetAsync("/api/v1/semesters");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update Semester (PUT)
        var updateReq = new UpdateSemesterRequest(
            Name: $"{semesterName}-ALT",
            OfferScheduleId: 2,
            EnrollmentScheduleId: 2
        );

        var putRes = await client.PutAsJsonAsync($"/api/v1/semesters/{semesterId}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Delete Semester (DELETE)
        var delRes = await client.DeleteAsync($"/api/v1/semesters/{semesterId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Courses_Crud_Flow_Should_Create_Get_Update_And_Delete_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        string courseName = $"Curso Teste {Guid.NewGuid().ToString("N")[..6]}";

        // 1. Create Course (POST)
        var createReq = new CreateCourseRequest(
            Name: courseName,
            Code: $"CRS-{Guid.NewGuid().ToString("N")[..4]}",
            PassingGrade: 7.0,
            MinGradeToFinalExam: 4.0,
            MinFinalExamGrade: 4.0,
            FinalExamPassingGrade: 5.0,
            MinHours: 64
        );

        var createRes = await client.PostAsJsonAsync("/api/v1/courses", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var courseJson = await createRes.Content.ReadFromJsonAsync<CourseTestDto>();
        courseJson.Should().NotBeNull();
        long courseId = courseJson!.Id;

        // 2. Get Courses (GET)
        var listRes = await client.GetAsync("/api/v1/courses");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update Course (PUT)
        var updateReq = new UpdateCourseRequest(
            Name: $"{courseName} Atualizado",
            Code: $"CRS-{Guid.NewGuid().ToString("N")[..4]}",
            PassingGrade: 7.5,
            MinGradeToFinalExam: 4.0,
            MinFinalExamGrade: 4.0,
            FinalExamPassingGrade: 5.0,
            MinHours: 80
        );

        var putRes = await client.PutAsJsonAsync($"/api/v1/courses/{courseId}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Delete Course (DELETE)
        var delRes = await client.DeleteAsync($"/api/v1/courses/{courseId}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record GroupTestDto(long Id, long OfferId, string? Code, string? Name);
    private record AllocationTestDto(long Id, long UserId, int ProfileId);
    private record SemesterTestDto(long Id, string Name);
    private record CourseTestDto(long Id, string? Code, string Name);
}
