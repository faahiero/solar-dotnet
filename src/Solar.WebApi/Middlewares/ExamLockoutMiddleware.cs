using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Middlewares;

/// <summary>
/// Middleware anti-fraude que bloqueia a navegação de alunos em materiais de aula, fóruns e arquivos
/// enquanto o aluno estiver realizando uma prova ativa com trava de conteúdo ativada (block_content = true).
/// Mapeado a partir de Exam.verify_blocking_content em app/models/exam.rb:460.
/// </summary>
public class ExamLockoutMiddleware
{
    private readonly RequestDelegate _next;

    public ExamLockoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SolarDbContext db)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Rotas isentas de bloqueio: autenticação, finalização da própria prova, health check, swagger
        if (path.StartsWith("/api/v1/auth") ||
            path.StartsWith("/api/v1/exams") ||
            path.StartsWith("/health") ||
            path.StartsWith("/openapi") ||
            path.StartsWith("/hubs/chat"))
        {
            await _next(context);
            return;
        }

        // Se houver um cabeçalho ou claim de identificação do aluno
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
            long.TryParse(userIdHeader, out long userId))
        {
            // Checar se o aluno possui tentativa incompleta em prova com trava de conteúdo
            bool hasActiveLockedExam = await (
                from attempt in db.ExamUserAttempts
                join acu in db.AcademicAllocationUsers on attempt.AcademicAllocationUserId equals acu.Id
                join ac in db.AcademicAllocations on acu.AcademicAllocationId equals ac.Id
                join exam in db.Exams on ac.AcademicToolId equals exam.Id
                where acu.UserId == userId
                      && !attempt.Complete
                      && exam.BlockContent
                      && ac.AcademicToolType == "Exam"
                select attempt.Id
            ).AnyAsync();

            if (hasActiveLockedExam)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Acesso bloqueado por segurança.",
                    code = "EXAM_CONTENT_LOCKED",
                    message = "Você possui uma prova online em andamento com bloqueio de navegação ativo. Finalize a prova para liberar o acesso aos outros conteúdos."
                });
                return;
            }
        }

        await _next(context);
    }
}
