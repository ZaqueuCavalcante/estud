using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Institutions.GetInstitutionNoteTypes;

public class GetInstitutionNoteTypesService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetInstitutionNoteTypesOut> Get()
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var gradeRule = await ctx.InstitutionConfigs.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .Select(x => x.GradeRule)
            .FirstAsync();

        return new GetInstitutionNoteTypesOut { NoteTypes = [.. gradeRule.NoteTypes] };
    }
}
