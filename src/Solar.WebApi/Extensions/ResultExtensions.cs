using Microsoft.AspNetCore.Mvc;
using Solar.Domain.Common;

namespace Solar.WebApi.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return MapErrorToProblemDetails(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new { Success = true });
        }

        return MapErrorToProblemDetails(result.Error);
    }

    private static IResult MapErrorToProblemDetails(Error error)
    {
        var problemDetails = new ProblemDetails
        {
            Detail = error.Description,
            Extensions = { ["code"] = error.Code }
        };

        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso Não Encontrado",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/not-found",
                Extensions = { ["code"] = error.Code }
            }),

            ErrorType.Conflict => Results.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflito de Dados",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/conflict",
                Extensions = { ["code"] = error.Code }
            }),

            ErrorType.Unauthorized => Results.Json(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não Autorizado",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/unauthorized",
                Extensions = { ["code"] = error.Code }
            }, statusCode: StatusCodes.Status401Unauthorized),

            ErrorType.Forbidden => Results.Json(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acesso Proibido",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/forbidden",
                Extensions = { ["code"] = error.Code }
            }, statusCode: StatusCodes.Status403Forbidden),

            ErrorType.Validation => Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Erro de Validação",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/validation",
                Extensions = { ["code"] = error.Code }
            }),

            _ => Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Falha na Operação",
                Detail = error.Description,
                Type = "https://solar.virtual.ufc.br/errors/bad-request",
                Extensions = { ["code"] = error.Code }
            })
        };
    }
}
