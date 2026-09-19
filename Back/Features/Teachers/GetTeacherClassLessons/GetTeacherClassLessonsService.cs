namespace Estud.Back.Features.Teachers.GetTeacherClassLessons;

public class GetTeacherClassLessonsService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<GetTeacherClassLessonsIn>
    {
        public Validator()
        {
            RuleFor(x => x.Search).Must(s => s.IsEmpty() || s!.Trim().Length is >= 3 and <= 100).WithError(InvalidClassLessonSearch.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<GetTeacherClassLessonsOut, EstudError>> Get(int classId, GetTeacherClassLessonsIn query)
    {
        if (V.Run(query, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var search = query.Search?.Trim();
        var lessonsQuery = search.HasValue() ? ctx.SearchClassLessons(search!) : ctx.ClassLessons;

        var lessons = await lessonsQuery.AsNoTracking()
            .Where(l => l.ClassId == classId)
            .OrderBy(l => l.Number)
            .Select(l => new GetTeacherClassLessonsItemOut
            {
                Id = l.Id,
                Number = l.Number,
                Date = l.Date,
                StartAt = l.StartAt,
                EndAt = l.EndAt,
                Status = l.Status,
                PlannedContent = l.PlannedContent,
                PresentStudents = l.Attendances.Where(a => a.Present).Select(a => a.StudentId).ToList(),
            })
            .ToListAsync();

        return new GetTeacherClassLessonsOut { Lessons = lessons };
    }
}
