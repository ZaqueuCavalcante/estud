namespace Estud.Back.Features.Students.GetStudentCurrentClasses;

public class GetStudentCurrentClassesService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetStudentCurrentClassesOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classes = await ctx.ClassStudents.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.Status == StudentClassStatus.Matriculado
                && x.Class!.InstitutionId == institutionId && x.Class.Status == ClassStatus.Started)
            .OrderBy(x => x.Class!.Discipline.Name)
            .Select(x => new GetStudentCurrentClassesItemOut
            {
                Id = x.ClassId,
                Name = x.Class!.Discipline.Name,
            })
            .ToListAsync();

        return new GetStudentCurrentClassesOut { Classes = classes };
    }
}
