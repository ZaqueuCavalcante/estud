namespace Estud.Back.Features.Classes.UpdateClassTeachers;

public class UpdateClassTeachersService(EstudDbContext ctx) : IEstudService
{
    private const int MaxTeachers = 2;

    private class Validator : AbstractValidator<UpdateClassTeachersIn>
    {
        public Validator()
        {
            RuleFor(x => x.Teachers)
                .Must(x => x != null && x.Count <= MaxTeachers && x.IsAllDistinct())
                .WithError(InvalidTeachersList.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Update(int classId, UpdateClassTeachersIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes
            .Include(c => c.Teachers)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var targetTeachers = await ctx.Teachers
            .Where(t => t.InstitutionId == institutionId && data.Teachers.Contains(t.Id))
            .ToListAsync();
        if (targetTeachers.Count != data.Teachers.Count) return TeacherNotFound.I;

        var teachersInDiscipline = await ctx.TeachersDisciplines
            .CountAsync(td => data.Teachers.Contains(td.TeacherId) && td.DisciplineId == @class.DisciplineId);
        if (teachersInDiscipline != data.Teachers.Count) return TeacherNotAssignedToDiscipline.I;

        @class.UpdateTeachers(targetTeachers);

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
