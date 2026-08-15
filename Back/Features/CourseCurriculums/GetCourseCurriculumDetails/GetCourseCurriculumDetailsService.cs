namespace Estud.Back.Features.CourseCurriculums.GetCourseCurriculumDetails;

public class GetCourseCurriculumDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetCourseCurriculumDetailsOut, EstudError>> Get(int courseCurriculumId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var curriculum = await ctx.CourseCurriculums.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && c.Id == courseCurriculumId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CourseId,
                Course = c.Course!.Name,
                c.Course.CourseType,
                Disciplines = c.Links
                    .OrderBy(l => l.Period)
                    .ThenBy(l => l.Discipline!.Name)
                    .Select(l => new GetCourseCurriculumDetailsDisciplineOut
                    {
                        Id = l.DisciplineId,
                        Name = l.Discipline!.Name,
                        Code = l.Discipline.Code,
                        Period = l.Period,
                        Credits = l.Credits,
                        Workload = l.Workload,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        if (curriculum == null) return CourseCurriculumNotFound.I;

        var offerings = await ctx.CourseOfferings.AsNoTracking()
            .Where(o => o.InstitutionId == institutionId && o.CourseCurriculumId == courseCurriculumId)
            .OrderByDescending(o => o.AcademicPeriod!.Name)
            .ThenBy(o => o.Campus!.Name)
            .Select(o => new GetCourseCurriculumDetailsOfferingOut
            {
                Id = o.Id,
                Campus = o.Campus!.Name,
                Period = o.AcademicPeriod!.Name,
                Session = o.Session,
                Students = ctx.StudentCourseEnrollments.Count(e => e.CourseOfferingId == o.Id && e.LeftAt == null),
            })
            .ToListAsync();

        var students = await ctx.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.LeftAt == null
                && e.CourseOffering!.InstitutionId == institutionId
                && e.CourseOffering.CourseCurriculumId == courseCurriculumId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync();

        return new GetCourseCurriculumDetailsOut
        {
            Id = curriculum.Id,
            Name = curriculum.Name,
            CourseId = curriculum.CourseId,
            Course = curriculum.Course,
            CourseType = curriculum.CourseType.GetDescription(),
            Periods = curriculum.Disciplines.Count == 0 ? 0 : curriculum.Disciplines.Max(d => d.Period),
            TotalCredits = curriculum.Disciplines.Sum(d => d.Credits),
            TotalWorkload = curriculum.Disciplines.Sum(d => d.Workload),
            Students = students,
            Disciplines = curriculum.Disciplines,
            Offerings = offerings,
        };
    }
}
