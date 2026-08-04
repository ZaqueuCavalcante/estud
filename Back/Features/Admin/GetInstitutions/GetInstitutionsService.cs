namespace Estud.Back.Features.Admin.GetInstitutions;

public class GetInstitutionsService(EstudDbContext ctx) : IEstudService
{
    private const int MaxPageSize = 100;

    public async Task<GetInstitutionsOut> Get(GetInstitutionsIn query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // Leitura cross-tenant: sem escopo por RequestUser (que fica 0/0 no host de admin).
        var institutionsQuery = ctx.Institutions.AsNoTracking();

        if (query.Name.HasValue())
            institutionsQuery = institutionsQuery.Where(i => EF.Functions.ILike(i.Name, $"%{query.Name}%"));

        var total = await institutionsQuery.CountAsync();

        var items = await institutionsQuery
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new GetInstitutionsItemOut
            {
                Id = i.Id,
                Name = i.Name,
                CreatedAt = i.CreatedAt,
                UsersCount = i.Users.Count,
            })
            .ToListAsync();

        return new GetInstitutionsOut
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items,
        };
    }
}
