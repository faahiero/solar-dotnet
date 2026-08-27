namespace Solar.WebApi.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").RequireAuthorization("AdminPolicy");

        group.MapAdminUserEndpoints();
        group.MapAdminGroupEndpoints();
        group.MapAdminAcademicStructureEndpoints();
        group.MapAdminObservabilityEndpoints();

        return app;
    }
}
