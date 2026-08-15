namespace Estud.Back.Features.Courses.GetCourseDetails;

public class GetCourseDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetCourseDetailsOut, EstudError>> Get(int courseId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var course = await ctx.Courses.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && c.Id == courseId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CourseType,
                Disciplines = c.Disciplines
                    .OrderBy(d => d.Name)
                    .Select(d => new GetCourseDetailsDisciplineOut { Id = d.Id, Name = d.Name, Code = d.Code })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        if (course == null) return CourseNotFound.I;

        var curriculums = await ctx.CourseCurriculums.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && c.CourseId == courseId)
            .OrderBy(c => c.Name)
            .Select(c => new GetCourseDetailsCurriculumOut
            {
                Id = c.Id,
                Name = c.Name,
                Disciplines = c.Links.Count,
            })
            .ToListAsync();

        var offerings = await ctx.CourseOfferings.AsNoTracking()
            .Where(o => o.InstitutionId == institutionId && o.CourseId == courseId)
            .OrderByDescending(o => o.AcademicPeriod!.Name)
            .ThenBy(o => o.Campus!.Name)
            .Select(o => new GetCourseDetailsOfferingOut
            {
                Id = o.Id,
                Campus = o.Campus!.Name,
                Curriculum = o.CourseCurriculum!.Name,
                Period = o.AcademicPeriod!.Name,
                Session = o.Session,
                Students = ctx.StudentCourseEnrollments.Count(e => e.CourseOfferingId == o.Id && e.LeftAt == null),
            })
            .ToListAsync();

        var students = await ctx.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.LeftAt == null
                && e.CourseOffering!.InstitutionId == institutionId
                && e.CourseOffering.CourseId == courseId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync();

        return new GetCourseDetailsOut
        {
            Id = course.Id,
            Name = course.Name,
            Type = course.CourseType.GetDescription(),
            TypeValue = course.CourseType,
            Students = students,
            Disciplines = course.Disciplines,
            Curriculums = curriculums,
            Offerings = offerings,
        };
    }
}
