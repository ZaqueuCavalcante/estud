namespace Estud.Back.Features.Disciplines.GetDisciplinePotentialTeachers;

public class GetDisciplinePotentialTeachersService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetDisciplinePotentialTeachersOut, EstudError>> Get(int disciplineId, string? name)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var disciplineOk = await ctx.Disciplines.AnyAsync(d => d.InstitutionId == institutionId && d.Id == disciplineId);
        if (!disciplineOk) return DisciplineNotFound.I;

        var query = ctx.Teachers.AsNoTracking()
            .Where(t => t.InstitutionId == institutionId && !t.Disciplines.Any(d => d.Id == disciplineId));

        if (name.HasValue()) query = query.Where(t => t.Name.ToLower().Contains(name.ToLower()));

        var items = await query
            .OrderBy(t => t.Name)
            .Select(t => new GetDisciplinePotentialTeacherItemOut { Id = t.Id, Name = t.Name })
            .ToListAsync();

        return new GetDisciplinePotentialTeachersOut { Items = items };
    }
}
