namespace Estud.Back.Features.Identity.GetSsoConfiguration;

public class GetSsoConfigurationService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetSsoConfigurationOut?> Get()
    {
        var config = await ctx.WebSsoConfigurations.AsNoTracking()
            .Include(x => x.AllowedDomains)
            .Where(x => x.InstitutionId == ctx.RequestUser.InstitutionId)
            .FirstOrDefaultAsync();

        return config?.ToGetSsoConfigurationOut();
    }
}
