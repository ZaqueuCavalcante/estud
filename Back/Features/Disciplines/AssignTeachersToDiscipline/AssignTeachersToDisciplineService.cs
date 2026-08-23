using Estud.Back.Domain.Teachers;

namespace Estud.Back.Features.Disciplines.AssignTeachersToDiscipline;

public class AssignTeachersToDisciplineService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Assign(int disciplineId, AssignTeachersToDisciplineIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var disciplineOk = await ctx.Disciplines.AnyAsync(d => d.InstitutionId == institutionId && d.Id == disciplineId);
        if (!disciplineOk) return DisciplineNotFound.I;

        var validTeacherIds = await ctx.Teachers
            .Where(t => t.InstitutionId == institutionId && data.Teachers.Contains(t.Id))
            .Select(t => t.Id).ToListAsync();

        if (validTeacherIds.Count != data.Teachers.Count) return InvalidTeachersList.I;

        var links = await ctx.TeachersDisciplines.Where(td => td.DisciplineId == disciplineId).ToListAsync();
        var linkedIds = links.Select(l => l.TeacherId).ToHashSet();

        ctx.RemoveRange(links.Where(l => !validTeacherIds.Contains(l.TeacherId)));

        validTeacherIds.Where(id => !linkedIds.Contains(id)).ToList()
            .ForEach(id => ctx.Add(new TeacherDiscipline { TeacherId = id, DisciplineId = disciplineId }));

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
