namespace Estud.Back.Features.Teachers.GetTeacherHome;

public class GetTeacherHomeService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetTeacherHomeOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var classes = await ctx.Classes.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && c.Status != ClassStatus.Finalized && c.Teachers.Any(t => t.Id == teacherId))
            .OrderByDescending(c => c.Status)
            .ThenBy(c => c.Discipline.Name)
            .Select(c => new GetTeacherHomeClassOut
            {
                Id = c.Id,
                Discipline = c.Discipline.Name,
                Period = c.Period.Name,
                Campus = c.Campus != null ? c.Campus.Name : null,
                Status = c.Status,
                Students = ctx.ClassStudents.Count(cs => cs.ClassId == c.Id),
                Lessons = c.Lessons.Count,
                FinishedLessons = c.Lessons.Count(l => l.Status == ClassLessonStatus.Finalized),
            })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasCurrentEnrollmentPeriod = await ctx.EnrollmentPeriods.AsNoTracking()
            .AnyAsync(p => p.InstitutionId == institutionId && p.StartAt <= today && today <= p.EndAt);

        if (!hasCurrentEnrollmentPeriod)
        {
            foreach (var @class in classes)
            {
                if (@class.Status == ClassStatus.OnEnrollment)
                    @class.Status = ClassStatus.OnReview;
            }
        }

        var startedIds = classes.Where(c => c.Status == ClassStatus.Started).Select(c => c.Id).ToList();

        var students = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => startedIds.Contains(cs.ClassId))
            .Select(cs => cs.StudentId)
            .Distinct()
            .CountAsync();

        return new GetTeacherHomeOut
        {
            ActiveClasses = startedIds.Count,
            Students = students,
            Classes = classes,
        };
    }
}
