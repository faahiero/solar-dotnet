namespace Solar.WebApi.Endpoints;

public static class AcademicEndpoints
{
    public static IEndpointRouteBuilder MapAcademicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").RequireAuthorization();

        group.MapGradeEndpoints();
        group.MapLessonEndpoints();
        group.MapDiscussionEndpoints();
        group.MapAssignmentEndpoints();
        group.MapCurriculumUnitEndpoints();
        group.MapEditionEndpoints();

        return app;
    }
}
