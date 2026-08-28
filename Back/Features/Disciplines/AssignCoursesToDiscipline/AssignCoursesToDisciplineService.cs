using Estud.Back.Domain.Courses;

namespace Estud.Back.Features.Disciplines.AssignCoursesToDiscipline;

public class AssignCoursesToDisciplineService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Assign(int disciplineId, AssignCoursesToDisciplineIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var disciplineOk = await ctx.Disciplines.AnyAsync(d => d.InstitutionId == institutionId && d.Id == disciplineId);
        if (!disciplineOk) return DisciplineNotFound.I;

        var validCourseIds = await ctx.Courses
            .Where(c => c.InstitutionId == institutionId && data.Courses.Contains(c.Id))
            .Select(c => c.Id).ToListAsync();

        if (validCourseIds.Count != data.Courses.Count) return InvalidCoursesList.I;

        var links = await ctx.CoursesDisciplines.Where(cd => cd.DisciplineId == disciplineId).ToListAsync();
        var linkedIds = links.Select(l => l.CourseId).ToHashSet();

        ctx.RemoveRange(links.Where(l => !validCourseIds.Contains(l.CourseId)));

        validCourseIds.Where(id => !linkedIds.Contains(id)).ToList()
            .ForEach(id => ctx.Add(new CourseDiscipline { CourseId = id, DisciplineId = disciplineId }));

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
