namespace Estud.Back.Features.Institutions.GetInstitutionConfig;

public class GetInstitutionConfigService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetInstitutionConfigOut> Get()
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var institution = await ctx.Institutions.AsNoTracking()
            .Include(x => x.Config)
            .FirstAsync(x => x.Id == institutionId);

        return institution.ToGetInstitutionConfigOut();
    }
}
