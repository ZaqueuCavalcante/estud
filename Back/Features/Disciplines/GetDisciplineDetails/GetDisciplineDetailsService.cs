namespace Estud.Back.Features.Disciplines.GetDisciplineDetails;

public class GetDisciplineDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetDisciplineDetailsOut, EstudError>> Get(int disciplineId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var discipline = await ctx.Disciplines.AsNoTracking()
            .Include(d => d.Links)
            .FirstOrDefaultAsync(d => d.InstitutionId == institutionId && d.Id == disciplineId);
        if (discipline == null) return DisciplineNotFound.I;

        var courseIds = discipline.Links.Select(l => l.CourseId).ToList();

        var courses = await ctx.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new GetDisciplineDetailsCourseOut { Id = c.Id, Name = c.Name })
            .ToListAsync();

        var teachers = await ctx.Teachers.AsNoTracking()
            .Where(t => t.InstitutionId == institutionId && t.Disciplines.Any(d => d.Id == disciplineId))
            .OrderBy(t => t.Name)
            .Select(t => new GetDisciplineDetailsTeacherOut { Id = t.Id, Name = t.Name })
            .ToListAsync();

        var classes = await ctx.Classes.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && c.DisciplineId == disciplineId)
            .OrderByDescending(c => c.Period.Name).ThenBy(c => c.Id)
            .Select(c => new GetDisciplineDetailsClassOut
            {
                Id = c.Id,
                Period = c.Period.Name,
                Campus = c.Campus != null ? c.Campus.Name : null,
                Vacancies = c.Vacancies,
                Students = ctx.ClassStudents.Count(cs => cs.ClassId == c.Id),
                Workload = c.Workload,
                Status = c.Status,
            })
            .ToListAsync();

        // Mesma regra do GetClass: fora de um período de matrícula vigente, uma
        // turma liberada para matrícula é exibida como aguardando início.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (classes.Any(c => c.Status == ClassStatus.OnEnrollment))
        {
            var hasCurrentEnrollmentPeriod = await ctx.EnrollmentPeriods.AsNoTracking()
                .AnyAsync(p => p.InstitutionId == institutionId && p.StartAt <= today && today <= p.EndAt);

            if (!hasCurrentEnrollmentPeriod)
            {
                foreach (var @class in classes.Where(c => c.Status == ClassStatus.OnEnrollment))
                    @class.Status = ClassStatus.OnReview;
            }
        }

        return new GetDisciplineDetailsOut
        {
            Id = discipline.Id,
            Name = discipline.Name,
            Code = discipline.Code,
            Courses = courses,
            Teachers = teachers,
            Classes = classes,
        };
    }
}
