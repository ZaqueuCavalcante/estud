using Estud.Back.Domain.Courses;

namespace Estud.Back.Features.Courses.AssignDisciplinesToCourse;

public class AssignDisciplinesToCourseService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Assign(int courseId, AssignDisciplinesToCourseIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var courseOk = await ctx.Courses.AnyAsync(c => c.InstitutionId == institutionId && c.Id == courseId);
        if (!courseOk) return CourseNotFound.I;

        var validDisciplineIds = await ctx.Disciplines
            .Where(d => d.InstitutionId == institutionId && data.Disciplines.Contains(d.Id))
            .Select(d => d.Id).ToListAsync();

        if (validDisciplineIds.Count != data.Disciplines.Count) return InvalidDisciplinesList.I;

        var links = await ctx.CoursesDisciplines.Where(cd => cd.CourseId == courseId).ToListAsync();
        var linkedIds = links.Select(l => l.DisciplineId).ToHashSet();

        ctx.RemoveRange(links.Where(l => !validDisciplineIds.Contains(l.DisciplineId)));

        validDisciplineIds.Where(id => !linkedIds.Contains(id)).ToList()
            .ForEach(id => ctx.Add(new CourseDiscipline { CourseId = courseId, DisciplineId = id }));

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
