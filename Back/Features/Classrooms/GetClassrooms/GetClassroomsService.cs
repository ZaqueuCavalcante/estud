namespace Estud.Back.Features.Classrooms.GetClassrooms;

public class GetClassroomsService(EstudDbContext ctx) : IEstudService
{
    public async Task<List<GetClassroomsOut>> Get()
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        // Ordem alfabética vem do banco: a tela lista sala em card, e lista que
        // muda de posição a cada resposta esconde a sala de quem procura.
        var classrooms = await ctx.Classrooms
            .Include(x => x.Campus)
            .Where(x => x.InstitutionId == institutionId)
            .OrderBy(x => x.Name).ToListAsync();

        return classrooms.ConvertAll(c => new GetClassroomsOut
        {
            Id = c.Id,
            Name = c.Name,
            CampusId = c.CampusId,
            Capacity = c.Capacity,
            Campus = c.Campus.Name,
        });
    }
}
