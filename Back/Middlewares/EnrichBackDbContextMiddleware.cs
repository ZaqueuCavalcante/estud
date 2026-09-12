namespace Estud.Back.Middlewares;

public class EnrichBackDbContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext request, EstudDbContext ctx)
    {
        if (request.User.IsAuthenticated)
        {
            ctx.RequestUser.Id = request.User.Id;
            ctx.RequestUser.Permissions = request.User.Permissions;
            ctx.RequestUser.InstitutionId = request.User.InstitutionId;
        }

        ctx.Enrich(request.GetTargetControllerName());

        await next(request);
    }
}
