namespace Estud.Back.Features.Students.GetStudentClassLessons;

public class GetStudentClassLessonsService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<GetStudentClassLessonsIn>
    {
        public Validator()
        {
            RuleFor(x => x.Search).Must(s => s.IsEmpty() || s!.Trim().Length is >= 3 and <= 100).WithError(InvalidClassLessonSearch.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<GetStudentClassLessonsOut, EstudError>> Get(int classId, GetStudentClassLessonsIn query)
    {
        if (V.Run(query, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var lessonsQuery = ctx.ClassLessons.AsNoTracking()
            .Where(l => l.ClassId == classId);

        var search = query.Search?.Trim();
        if (search.HasValue())
        {
            lessonsQuery = lessonsQuery.Where(l => l.PlannedContent != null
                && EF.Functions.ToTsVector("portuguese", EF.Functions.Unaccent(l.PlannedContent))
                    .Matches(EF.Functions.WebSearchToTsQuery("portuguese", EF.Functions.Unaccent(search!))));
        }

        var lessons = await lessonsQuery
            .OrderBy(l => l.Number)
            .Select(l => new GetStudentClassLessonsItemOut
            {
                Id = l.Id,
                Number = l.Number,
                Date = l.Date,
                StartAt = l.StartAt,
                EndAt = l.EndAt,
                Status = l.Status,
            })
            .ToListAsync();

        return new GetStudentClassLessonsOut { Lessons = lessons };
    }
}
