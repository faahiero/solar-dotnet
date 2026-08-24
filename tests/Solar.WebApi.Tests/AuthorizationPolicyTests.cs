using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Domain.Academic;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class AuthorizationPolicyTests
{
    private SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(databaseName: $"SolarAuthTestDb_{Guid.NewGuid()}")
            .Options;

        return new SolarDbContext(options);
    }

    [Fact]
    public async Task AdminUser_Should_Have_Global_Access_To_All_Resources()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var scopeService = new AllocationTagScopeService();
        var authService = new SolarAuthorizationService(db, scopeService);

        var adminUser = new User { Username = "admin_master", Name = "Administrador Geral", Active = true };
        db.Users.Add(adminUser);

        var adminProfile = new Profile { Name = "Administrador Global", Types = ProfileType.Admin };
        db.Profiles.Add(adminProfile);
        await db.SaveChangesAsync();

        db.Allocations.Add(new Allocation
        {
            UserId = adminUser.Id,
            ProfileId = adminProfile.Id,
            Status = AllocationStatus.Activated
        });
        await db.SaveChangesAsync();

        // Act & Assert
        (await authService.HasGlobalAdminAccessAsync(adminUser.Id)).Should().BeTrue();
        (await authService.CanManageCurriculumUnitAsync(adminUser.Id, 999)).Should().BeTrue();
        (await authService.CanGradeStudentAsync(adminUser.Id, 888)).Should().BeTrue();
        (await authService.CanAccessGroupAsync(adminUser.Id, 777, writeAccess: true)).Should().BeTrue();
        (await authService.CanSubmitAssignmentAsync(adminUser.Id, 666)).Should().BeTrue();
    }

    [Fact]
    public async Task Teacher_Should_Only_Manage_Their_Assigned_CurriculumUnit()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var scopeService = new AllocationTagScopeService();
        var authService = new SolarAuthorizationService(db, scopeService);

        var teacher = new User { Username = "prof_carlos", Name = "Prof. Carlos", Active = true };
        db.Users.Add(teacher);

        var teacherProfile = new Profile { Name = "Professor Titular", Types = ProfileType.ClassResponsible };
        db.Profiles.Add(teacherProfile);

        var offer1 = new Offer { Id = 10, CurriculumUnitId = 100, SemesterId = 1 };
        var offer2 = new Offer { Id = 20, CurriculumUnitId = 200, SemesterId = 1 };
        db.Offers.AddRange(offer1, offer2);

        var tagOffer1 = new AllocationTag { Id = 101, OfferId = 10 };
        var tagOffer2 = new AllocationTag { Id = 201, OfferId = 20 };
        db.AllocationTags.AddRange(tagOffer1, tagOffer2);

        // Aloca o professor apenas na Oferta 10 (CurriculumUnit 100)
        db.Allocations.Add(new Allocation
        {
            UserId = teacher.Id,
            ProfileId = teacherProfile.Id,
            AllocationTagId = tagOffer1.Id,
            Status = AllocationStatus.Activated
        });
        await db.SaveChangesAsync();

        // Act
        var canManageAssigned = await authService.CanManageCurriculumUnitAsync(teacher.Id, 100);
        var canManageUnassigned = await authService.CanManageCurriculumUnitAsync(teacher.Id, 200);

        // Assert
        canManageAssigned.Should().BeTrue("o professor está alocado na Oferta da Unidade Curricular 100");
        canManageUnassigned.Should().BeFalse("o professor NÃO está alocado na Unidade Curricular 200");
    }

    [Fact]
    public async Task Teacher_Should_Only_Grade_Students_From_Same_Offering()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var scopeService = new AllocationTagScopeService();
        var authService = new SolarAuthorizationService(db, scopeService);

        var teacher = new User { Username = "prof_ana", Name = "Profª Ana", Active = true };
        var studentMyClass = new User { Username = "aluno_meu", Name = "Aluno da Turma", Active = true };
        var studentOtherClass = new User { Username = "aluno_outro", Name = "Aluno de Outro Curso", Active = true };
        db.Users.AddRange(teacher, studentMyClass, studentOtherClass);

        var teacherProfile = new Profile { Name = "Professor", Types = ProfileType.ClassResponsible };
        var studentProfile = new Profile { Name = "Aluno", Types = ProfileType.Student };
        db.Profiles.AddRange(teacherProfile, studentProfile);

        var tagOffer1 = new AllocationTag { Id = 501, OfferId = 50 };
        var tagOffer2 = new AllocationTag { Id = 601, OfferId = 60 };
        db.AllocationTags.AddRange(tagOffer1, tagOffer2);

        // Teacher na Offer 50
        db.Allocations.Add(new Allocation { UserId = teacher.Id, ProfileId = teacherProfile.Id, AllocationTagId = tagOffer1.Id, Status = AllocationStatus.Activated });
        // Aluno 1 na Offer 50
        db.Allocations.Add(new Allocation { UserId = studentMyClass.Id, ProfileId = studentProfile.Id, AllocationTagId = tagOffer1.Id, Status = AllocationStatus.Activated });
        // Aluno 2 na Offer 60
        db.Allocations.Add(new Allocation { UserId = studentOtherClass.Id, ProfileId = studentProfile.Id, AllocationTagId = tagOffer2.Id, Status = AllocationStatus.Activated });
        await db.SaveChangesAsync();

        // Act
        var canGradeStudent1 = await authService.CanGradeStudentAsync(teacher.Id, studentMyClass.Id);
        var canGradeStudent2 = await authService.CanGradeStudentAsync(teacher.Id, studentOtherClass.Id);

        // Assert
        canGradeStudent1.Should().BeTrue("ambos compartilham a mesma oferta acadêmica");
        canGradeStudent2.Should().BeFalse("o aluno pertence a uma oferta diferente");
    }

    [Fact]
    public async Task Student_Should_Not_Have_Write_Access_To_Manage_Groups()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var scopeService = new AllocationTagScopeService();
        var authService = new SolarAuthorizationService(db, scopeService);

        var student = new User { Username = "aluno_leitor", Name = "Aluno Leitor", Active = true };
        db.Users.Add(student);

        var studentProfile = new Profile { Name = "Aluno", Types = ProfileType.Student };
        db.Profiles.Add(studentProfile);

        var group = new Group { Id = 300, OfferId = 30, Name = "Turma A" };
        db.Groups.Add(group);

        var tagGroup = new AllocationTag { Id = 301, GroupId = 300, OfferId = 30 };
        db.AllocationTags.Add(tagGroup);

        db.Allocations.Add(new Allocation
        {
            UserId = student.Id,
            ProfileId = studentProfile.Id,
            AllocationTagId = tagGroup.Id,
            Status = AllocationStatus.Activated
        });
        await db.SaveChangesAsync();

        // Act
        var canRead = await authService.CanAccessGroupAsync(student.Id, 300, writeAccess: false);
        var canWrite = await authService.CanAccessGroupAsync(student.Id, 300, writeAccess: true);

        // Assert
        canRead.Should().BeTrue("o aluno tem permissão de visualização na sua turma");
        canWrite.Should().BeFalse("o aluno não tem permissão de escrita/edição estrutural da turma");
    }
}
