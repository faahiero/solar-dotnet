using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Filters;

namespace Solar.WebApi.Endpoints;

public record CreateSemesterRequest(string Name, long? OfferScheduleId, long? EnrollmentScheduleId);
public record UpdateSemesterRequest(string? Name, long? OfferScheduleId, long? EnrollmentScheduleId);

public record CreateCourseRequest(string Name, string? Code, double? PassingGrade, double? MinGradeToFinalExam, double? MinFinalExamGrade, double? FinalExamPassingGrade, int? MinHours);
public record UpdateCourseRequest(string? Name, string? Code, double? PassingGrade, double? MinGradeToFinalExam, double? MinFinalExamGrade, double? FinalExamPassingGrade, int? MinHours);

public static class AdminAcademicStructureEndpoints
{
    public static IEndpointRouteBuilder MapAdminAcademicStructureEndpoints(this IEndpointRouteBuilder group)
    {
        // ----------------------------------------------------
        // CRUD de Semestres (Semesters - Espelha semesters_controller)
        // ----------------------------------------------------
        group.MapGet("/api/v1/semesters", async (SolarDbContext db) =>
        {
            var semesters = await db.Semesters
                .OrderByDescending(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.OfferScheduleId,
                    s.EnrollmentScheduleId,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(semesters);
        })
        .WithName("GetSemesters")
        .WithSummary("Lista os semestres acadêmicos");

        group.MapGet("/api/v1/semesters/{id}", async (long id, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            return Results.Ok(new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.CreatedAt,
                semester.UpdatedAt
            });
        })
        .WithName("GetSemesterById")
        .WithSummary("Retorna os detalhes de um semestre");

        group.MapPost("/api/v1/semesters", async (CreateSemesterRequest req, SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Nome do semestre é obrigatório." });

            var semester = new Semester
            {
                Name = req.Name,
                OfferScheduleId = req.OfferScheduleId ?? 0,
                EnrollmentScheduleId = req.EnrollmentScheduleId ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Semesters.Add(semester);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/semesters/{semester.Id}", new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.CreatedAt
            });
        })
        .AddEndpointFilter<ValidationFilter<CreateSemesterRequest>>()
        .WithName("CreateSemester")
        .WithSummary("Cria um novo semestre acadêmico");

        group.MapPut("/api/v1/semesters/{id}", async (long id, UpdateSemesterRequest req, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            if (!string.IsNullOrWhiteSpace(req.Name)) semester.Name = req.Name;
            if (req.OfferScheduleId.HasValue) semester.OfferScheduleId = req.OfferScheduleId.Value;
            if (req.EnrollmentScheduleId.HasValue) semester.EnrollmentScheduleId = req.EnrollmentScheduleId.Value;
            semester.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.UpdatedAt
            });
        })
        .WithName("UpdateSemester")
        .WithSummary("Atualiza um semestre acadêmico");

        group.MapDelete("/api/v1/semesters/{id}", async (long id, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            db.Semesters.Remove(semester);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Semestre removido com sucesso." });
        })
        .WithName("DeleteSemester")
        .WithSummary("Exclui um semestre");

        // ----------------------------------------------------
        // CRUD de Cursos (Courses - Espelha courses_controller)
        // ----------------------------------------------------
        group.MapGet("/api/v1/courses", async (SolarDbContext db) =>
        {
            var courses = await db.Courses
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Name,
                    c.PassingGrade,
                    c.MinGradeToFinalExam,
                    c.MinFinalExamGrade,
                    c.FinalExamPassingGrade,
                    c.MinHours,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(courses);
        })
        .WithName("GetCourses")
        .WithSummary("Lista os cursos acadêmicos");

        group.MapGet("/api/v1/courses/{id}", async (long id, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            return Results.Ok(new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.MinGradeToFinalExam,
                course.MinFinalExamGrade,
                course.FinalExamPassingGrade,
                course.MinHours,
                course.CreatedAt,
                course.UpdatedAt
            });
        })
        .WithName("GetCourseById")
        .WithSummary("Retorna os detalhes de um curso");

        group.MapPost("/api/v1/courses", async (CreateCourseRequest req, SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Nome do curso é obrigatório." });

            var course = new Course
            {
                Code = req.Code,
                Name = req.Name,
                PassingGrade = req.PassingGrade ?? 7.0,
                MinGradeToFinalExam = req.MinGradeToFinalExam ?? 4.0,
                MinFinalExamGrade = req.MinFinalExamGrade ?? 4.0,
                FinalExamPassingGrade = req.FinalExamPassingGrade ?? 5.0,
                MinHours = req.MinHours ?? 64,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Courses.Add(course);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/courses/{course.Id}", new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.MinGradeToFinalExam,
                course.MinFinalExamGrade,
                course.FinalExamPassingGrade,
                course.MinHours,
                course.CreatedAt
            });
        })
        .AddEndpointFilter<ValidationFilter<CreateCourseRequest>>()
        .WithName("CreateCourse")
        .WithSummary("Cria um novo curso acadêmico");

        group.MapPut("/api/v1/courses/{id}", async (long id, UpdateCourseRequest req, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            if (!string.IsNullOrWhiteSpace(req.Name)) course.Name = req.Name;
            if (req.Code != null) course.Code = req.Code;
            if (req.PassingGrade.HasValue) course.PassingGrade = req.PassingGrade.Value;
            if (req.MinGradeToFinalExam.HasValue) course.MinGradeToFinalExam = req.MinGradeToFinalExam.Value;
            if (req.MinFinalExamGrade.HasValue) course.MinFinalExamGrade = req.MinFinalExamGrade.Value;
            if (req.FinalExamPassingGrade.HasValue) course.FinalExamPassingGrade = req.FinalExamPassingGrade.Value;
            if (req.MinHours.HasValue) course.MinHours = req.MinHours.Value;
            course.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.MinGradeToFinalExam,
                course.MinFinalExamGrade,
                course.FinalExamPassingGrade,
                course.MinHours,
                course.UpdatedAt
            });
        })
        .WithName("UpdateCourse")
        .WithSummary("Atualiza dados de um curso");

        group.MapDelete("/api/v1/courses/{id}", async (long id, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            db.Courses.Remove(course);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Curso removido com sucesso." });
        })
        .WithName("DeleteCourse")
        .WithSummary("Exclui um curso");

        return group;
    }
}
